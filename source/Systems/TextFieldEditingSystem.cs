using Clipboard;
using Collections;
using Collections.Generic;
using Fonts;
using Meshes;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Transforms;
using Transforms.Components;
using UI.Components;
using UI.Functions;
using Unmanaged;
using Worlds;

namespace UI.Systems
{
    [SkipLocalsInit]
    public partial struct TextFieldEditingSystem : ISystem
    {
        private static readonly char[] controlCharacters = [' ', '.', ',', '_', '-', '+', '*', '/', '\n'];

        private readonly Dictionary<Entity, TextSelection> lastSelections;
        private PressedCharacters lastPressedCharacters;
        private FixedString currentCharacters;
        private DateTime nextPress;
        private bool lastAnyPointerPressed;
        private readonly Library clipboard;

        private TextFieldEditingSystem(Library clipboard)
        {
            lastSelections = new();
            this.clipboard = clipboard;
            lastPressedCharacters = default;
            currentCharacters = default;
            nextPress = DateTime.UtcNow;
            lastAnyPointerPressed = false;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new TextFieldEditingSystem(new Library()));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            Update(world);
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                clipboard.Dispose();
                lastSelections.Dispose();
            }
        }

        private void Update(World world)
        {
            ComponentQuery<IsTextField, LocalToWorld> textLabelQuery = new(world);
            foreach (var r in textLabelQuery)
            {
                uint entity = r.entity;
                ref IsTextField component = ref r.component1;
                ref LocalToWorld ltw = ref r.component2;
                uint labelEntity = world.GetReference(entity, component.textLabelReference);
                uint cursorEntity = world.GetReference(entity, component.cursorReference);
                uint highlightEntity = world.GetReference(entity, component.highlightReference);
                Canvas canvas = new Entity(world, entity).GetCanvas();
                Vector3 position = ltw.Position;
                Vector3 scale = ltw.Scale;
                Vector2 destinationSize = canvas.Size;
                Vector4 region = new(position.X, destinationSize.Y - position.Y - scale.Y, scale.X, scale.Y);
                world.SetComponent(labelEntity, new RendererScissor(region));
                world.SetComponent(cursorEntity, new RendererScissor(region));
                world.SetComponent(highlightEntity, new RendererScissor(region));
            }

            if (world.TryGetFirst(out Settings settings))
            {
                ComponentQuery<IsPointer> pointerQuery = new(world);
                bool currentAnyPointerPressed = false;
                uint firstPressedPointer = default;
                foreach (var p in pointerQuery)
                {
                    currentAnyPointerPressed |= p.component1.HasPrimaryIntent;
                    if (currentAnyPointerPressed && firstPressedPointer == default)
                    {
                        firstPressedPointer = p.entity;
                    }
                }

                bool anyPointerPressed = false;
                if (currentAnyPointerPressed != lastAnyPointerPressed)
                {
                    lastAnyPointerPressed = currentAnyPointerPressed;
                    anyPointerPressed = currentAnyPointerPressed;
                }

                PressedCharacters pressedCharacters = settings.PressedCharacters;
                ref TextSelection selection = ref settings.TextSelection;
                bool pressedControl = pressedCharacters.Contains(Settings.ControlCharacter);
                bool editingAny = false;
                bool startedEditing = false;
                DateTime now = DateTime.UtcNow;
                ulong ticks = (ulong)((now - DateTime.UnixEpoch).TotalSeconds * 3f);

                ComponentQuery<IsTextField, IsSelectable> textFieldQuery = new(world);
                foreach (var t in textFieldQuery)
                {
                    uint textFieldEntity = t.entity;
                    ref IsTextField component = ref t.component1;
                    ref IsSelectable selectable = ref t.component2;
                    bool startEditing = selectable.WasPrimaryInteractedWith;
                    rint cursorReference = component.cursorReference;
                    uint cursorEntity = world.GetReference(textFieldEntity, cursorReference);
                    rint textLabelReference = component.textLabelReference;
                    uint textLabelEntity = world.GetReference(textFieldEntity, textLabelReference);
                    Label textLabel = new Entity(world, textLabelEntity).As<Label>();

                    if (startEditing && !startedEditing)
                    {
                        if (anyPointerPressed)
                        {
                            ComponentQuery<IsTextField> otherTextFieldQuery = new(world);
                            foreach (var otherT in otherTextFieldQuery)
                            {
                                ref IsTextField otherComponent = ref otherT.component1;
                                otherComponent.editing = false;
                            }

                            Pointer pointer = new Entity(world, firstPressedPointer).As<Pointer>();
                            StartEditing(world, textFieldEntity, pointer, pressedCharacters, ref selection);
                            startedEditing = true;
                            if (component.beginEditing != default)
                            {
                                component.beginEditing.Invoke(new Entity(world, textFieldEntity).As<Label>());
                            }
                        }
                    }

                    if (component.editing)
                    {
                        editingAny = true;
                        bool enableCursor = (ticks + textFieldEntity) % 2 == 0;
                        world.SetEnabled(cursorEntity, enableCursor);
                        bool charactersChanged = false;
                        if (lastPressedCharacters != pressedCharacters)
                        {
                            if (pressedControl)
                            {
                                if (pressedCharacters.Contains('x') || pressedCharacters.Contains('c'))
                                {
                                    uint start = Math.Min(selection.start, selection.end);
                                    uint end = Math.Max(selection.start, selection.end);
                                    uint length = end - start;
                                    if (length > 0)
                                    {
                                        clipboard.Text = textLabel.ProcessedText.Slice(start, length).ToString();
                                        if (pressedCharacters.Contains('x'))
                                        {
                                            RemoveSelection(textLabel, component.validation, ref selection);
                                        }
                                    }
                                }
                                else if (pressedCharacters.Contains('v'))
                                {
                                    if (clipboard.Text is string clipboardText)
                                    {
                                        InsertText(textLabel, clipboardText, ref selection);
                                    }
                                }
                                else if (pressedCharacters.Contains('a'))
                                {
                                    selection = new(0, textLabel.ProcessedText.Length, textLabel.ProcessedText.Length);
                                }
                            }

                            currentCharacters = default;
                            for (uint i = 0; i < pressedCharacters.Length; i++)
                            {
                                char c = pressedCharacters[i];
                                if (!lastPressedCharacters.Contains(c))
                                {
                                    currentCharacters.Append(c);
                                }
                            }

                            lastPressedCharacters = pressedCharacters;
                            charactersChanged = true;
                        }

                        if (currentCharacters != default && (now >= nextPress || charactersChanged))
                        {
                            nextPress = now + TimeSpan.FromMilliseconds(50);
                            for (uint i = 0; i < currentCharacters.Length; i++)
                            {
                                char c = currentCharacters[i];
                                if (pressedControl)
                                {
                                    if (c == 'x' || c == 'c' || c == 'v' || c == 'a')
                                    {
                                        //skip
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (c == Settings.EnterCharacter)
                                    {
                                        if (component.submit != default)
                                        {
                                            if (component.submit.Invoke(new Entity(world, textFieldEntity).As<Label>(), settings))
                                            {
                                                component.editing = false;
                                                continue;
                                            }
                                        }
                                    }
                                    else if (c == Settings.EscapeCharacter)
                                    {
                                        if (component.cancel != default)
                                        {
                                            component.cancel.Invoke(new Entity(world, textFieldEntity).As<Label>());
                                        }

                                        component.editing = false;
                                        continue;
                                    }
                                }

                                HandleCharacter(world, textLabel, component.validation, c, pressedCharacters, ref selection);
                            }
                        }

                        if (charactersChanged)
                        {
                            nextPress = now + TimeSpan.FromMilliseconds(500);
                        }

                        //update cursor to match position
                        UpdateCursorToMatchPosition(world, textLabel, cursorEntity, ref selection);

                        rint highlightReference = component.highlightReference;
                        uint highlightEntity = world.GetReference(textFieldEntity, highlightReference);
                        UpdateHighlightToMatchPosition(world, textLabel, new Entity(world, highlightEntity).As<Label>(), ref selection);
                    }
                    else
                    {
                        world.SetEnabled(cursorEntity, false);
                    }
                }

                if (!editingAny)
                {
                    settings.TextSelection = default;
                }

                if (anyPointerPressed && !startedEditing)
                {
                    //stop editing because we didnt press a text field
                    settings.TextSelection = default;
                    foreach (var t in textFieldQuery)
                    {
                        ref IsTextField component = ref t.component1;
                        component.editing = false;
                    }
                }
            }
        }

        private static void InsertText(Label textLabel, string clipboardText, ref TextSelection selection)
        {
            uint clipboardLength = (uint)clipboardText.Length;
            USpan<char> newText = stackalloc char[(int)(textLabel.ProcessedText.Length + clipboardLength)];
            USpan<char> text = textLabel.ProcessedText;
            uint insertIndex = selection.index;
            if (selection.start != selection.end)
            {
                insertIndex = Math.Min(selection.start, selection.end);
            }

            text.Slice(0, insertIndex).CopyTo(newText);
            clipboardText.AsSpan().CopyTo(newText.Slice(insertIndex));
            text.Slice(insertIndex).CopyTo(newText.Slice(insertIndex + clipboardLength));
            textLabel.SetText(newText);
            selection.index += clipboardLength;
        }

        private static void StartEditing(World world, uint textFieldEntity, Pointer pointer, PressedCharacters pressedCharacters, ref TextSelection selection)
        {
            TextField textField = new Entity(world, textFieldEntity).As<TextField>();
            textField.Editing = true;
            Vector3 worldPosition = textField.As<Transform>().WorldPosition;
            Vector2 pointerPosition = pointer.Position;
            pointerPosition.X -= worldPosition.X;
            pointerPosition.Y -= worldPosition.Y;

            Label textLabel = textField.TextLabel;
            USpan<char> text = textLabel.ProcessedText;
            USpan<char> tempText = stackalloc char[(int)(text.Length + 1)];
            text.CopyTo(tempText);
            tempText[text.Length] = ' ';
            if (textLabel.Font.TryIndexOf(tempText, pointerPosition / textLabel.Size, out uint newIndex))
            {
                bool holdingShift = pressedCharacters.Contains(Settings.ShiftCharacter);
                if (holdingShift)
                {
                    uint start = Math.Min(selection.start, selection.end);
                    uint end = Math.Max(selection.start, selection.end);
                    uint length = end - start;
                    if (length == 0)
                    {
                        selection.start = selection.index;
                    }

                    selection.end = newIndex;
                }
                else
                {
                    selection.start = 0;
                    selection.end = 0;
                }

                selection.index = newIndex;
            }
        }

        private static void UpdateCursorToMatchPosition(World world, Label textLabel, uint cursorEntity, ref TextSelection selection)
        {
            Font font = textLabel.Font;
            USpan<char> text = textLabel.ProcessedText;

            uint index = Math.Min(selection.index, text.Length);

            USpan<char> textToCursor = text.Slice(0, index);
            Vector2 totalSize = font.CalulcateSize(textToCursor) * textLabel.Size;
            Vector3 cursorSize = world.GetComponent<Scale>(cursorEntity).value;

            ref Position cursorPosition = ref world.GetComponent<Position>(cursorEntity);
            LocalToWorld ltw = world.GetComponent<LocalToWorld>(cursorEntity);
            Vector3 offset = world.GetComponent<Position>(textLabel.value).value;
            Vector3 worldPosition = ltw.Position + new Vector3(totalSize.X + offset.X, -(totalSize.Y + cursorSize.Y - offset.Y), 0) * cursorSize;
            Matrix4x4.Invert(ltw.value, out Matrix4x4 inverseLtw);
            Vector3 localPosition = Vector3.Transform(worldPosition, inverseLtw);

            localPosition.Z = cursorPosition.value.Z;
            cursorPosition.value = localPosition;
        }

        private readonly void UpdateHighlightToMatchPosition(World world, Label textLabel, Entity highlightEntity, ref TextSelection selection)
        {
            ref TextSelection lastSelection = ref lastSelections.TryGetValue(highlightEntity, out bool contains);
            if (contains && lastSelection == selection)
            {
                return;
            }
            else if (!contains)
            {
                lastSelections.Add(highlightEntity, selection);
            }
            else
            {
                lastSelection = selection;
            }

            uint start = Math.Min(selection.start, selection.end);
            uint end = Math.Max(selection.start, selection.end);
            uint length = end - start;
            bool showHighlight = length > 0;
            highlightEntity.IsEnabled = showHighlight;
            if (showHighlight)
            {
                //unique mesh per highlight
                Vector2 fontSize = textLabel.Size;
                Font font = textLabel.Font;
                USpan<char> text = textLabel.ProcessedText;
                Vector2 offset = new(4f, -4f);
                Vector2 padding = new(2f, 2f);
                ref IsRenderer renderer = ref highlightEntity.GetComponent<IsRenderer>();
                rint meshReference = renderer.meshReference;
                uint meshEntity = highlightEntity.GetReference(meshReference);
                uint lineStart = 0;
                using Array<Vector3> verticesList = new(text.Length * 4);
                using Array<uint> indicesList = new(text.Length * 6);
                using Array<Vector2> uvList = new(text.Length * 4);
                using Array<Vector3> normalsList = new(text.Length * 4);
                using Array<Vector4> colorsList = new(text.Length * 4);
                float pixelSize = font.PixelSize;
                float lineHeight = (font.LineHeight * (pixelSize / 32f)) / Font.FixedPointScale / pixelSize;
                fontSize.Y *= lineHeight;
                uint faceCount = 0;
                bool atEnd = false;
                uint lineIndex = 0;
                while (!atEnd)
                {
                    if (!text.Slice(lineStart).TryIndexOf('\n', out uint lineLength))
                    {
                        lineLength = text.Length - lineStart;
                        atEnd = true;
                    }

                    USpan<char> line = text.Slice(lineStart, lineLength);
                    uint lineEnd = lineStart + lineLength;
                    Vector2 minPosition = default;
                    Vector2 maxPosition = default;
                    if (start >= lineStart && end <= lineEnd)
                    {
                        //selection starts and ends on this line
                        USpan<char> textToStart = line.Slice(0, start - lineStart);
                        USpan<char> textToEnd = line.Slice(0, end - lineStart);
                        minPosition = font.CalulcateSize(textToStart) * fontSize;
                        maxPosition = font.CalulcateSize(textToEnd) * fontSize;
                    }
                    else if (end >= lineStart && end <= lineEnd)
                    {
                        //selection starts on previous line and ends on this one
                        USpan<char> textToEnd = line.Slice(0, end - lineStart);
                        maxPosition = font.CalulcateSize(textToEnd) * fontSize;
                    }
                    else if (start >= lineStart && start <= lineEnd)
                    {
                        //selection starts on this line, and ends later
                        USpan<char> textToStart = line.Slice(0, start - lineStart);
                        minPosition = font.CalulcateSize(textToStart) * fontSize;
                        maxPosition = font.CalulcateSize(line) * fontSize;
                    }
                    else if (lineStart >= start && end >= lineEnd)
                    {
                        //selection encompasses this line 
                        maxPosition = font.CalulcateSize(line) * fontSize;
                    }

                    if (minPosition != default || maxPosition != default)
                    {
                        minPosition.Y -= lineIndex * fontSize.Y;
                        maxPosition.Y -= lineIndex * fontSize.Y;
                        minPosition.Y -= fontSize.Y;
                        minPosition += offset - padding;
                        maxPosition += offset + padding;
                        verticesList[(faceCount * 4) + 0] = new(minPosition.X, minPosition.Y, 0);
                        verticesList[(faceCount * 4) + 1] = new(maxPosition.X, minPosition.Y, 0);
                        verticesList[(faceCount * 4) + 2] = new(maxPosition.X, maxPosition.Y, 0);
                        verticesList[(faceCount * 4) + 3] = new(minPosition.X, maxPosition.Y, 0);
                        uvList[(faceCount * 4) + 0] = new(0, 0);
                        uvList[(faceCount * 4) + 1] = new(1, 0);
                        uvList[(faceCount * 4) + 2] = new(1, 1);
                        uvList[(faceCount * 4) + 3] = new(0, 1);
                        normalsList[(faceCount * 4) + 0] = new(0, 0, 1);
                        normalsList[(faceCount * 4) + 1] = new(0, 0, 1);
                        normalsList[(faceCount * 4) + 2] = new(0, 0, 1);
                        normalsList[(faceCount * 4) + 3] = new(0, 0, 1);
                        colorsList[(faceCount * 4) + 0] = new(1, 1, 1, 1);
                        colorsList[(faceCount * 4) + 1] = new(1, 1, 1, 1);
                        colorsList[(faceCount * 4) + 2] = new(1, 1, 1, 1);
                        colorsList[(faceCount * 4) + 3] = new(1, 1, 1, 1);
                        indicesList[(faceCount * 6) + 0] = (faceCount * 4) + 0;
                        indicesList[(faceCount * 6) + 1] = (faceCount * 4) + 1;
                        indicesList[(faceCount * 6) + 2] = (faceCount * 4) + 2;
                        indicesList[(faceCount * 6) + 3] = (faceCount * 4) + 2;
                        indicesList[(faceCount * 6) + 4] = (faceCount * 4) + 3;
                        indicesList[(faceCount * 6) + 5] = (faceCount * 4) + 0;
                        faceCount++;
                    }

                    lineStart = lineEnd + 1;
                    lineIndex++;
                }

                Mesh highlightMesh = new Entity(world, meshEntity).As<Mesh>();
                USpan<Vector3> positions = highlightMesh.ResizePositions(faceCount * 4);
                USpan<uint> indices = highlightMesh.ResizeIndices(faceCount * 6);
                USpan<Vector2> uvs = highlightMesh.ResizeUVs(faceCount * 4);
                USpan<Vector3> normals = highlightMesh.ResizeNormals(faceCount * 4);
                USpan<Vector4> colors = highlightMesh.ResizeColors(faceCount * 4);
                verticesList.AsSpan().Slice(0, faceCount * 4).CopyTo(positions);
                indicesList.AsSpan().Slice(0, faceCount * 6).CopyTo(indices);
                uvList.AsSpan().Slice(0, faceCount * 4).CopyTo(uvs);
                normalsList.AsSpan().Slice(0, faceCount * 4).CopyTo(normals);
                colorsList.AsSpan().Slice(0, faceCount * 4).CopyTo(colors);
            }
        }

        private static bool TryGetPreviousIndex(USpan<char> text, out uint index)
        {
            for (uint i = text.Length - 1; i != uint.MaxValue; i--)
            {
                if (System.Array.IndexOf(controlCharacters, text[i]) != -1)
                {
                    index = i;
                    return true;
                }
            }

            index = default;
            return false;
        }

        private static bool TryGetNextIndex(USpan<char> text, out uint index)
        {
            for (uint i = 0; i < text.Length; i++)
            {
                if (System.Array.IndexOf(controlCharacters, text[i]) != -1)
                {
                    index = i;
                    return true;
                }
            }

            index = default;
            return false;
        }

        private static uint GetLineNumber(USpan<char> text, uint index)
        {
            uint line = 0;
            for (uint i = 0; i < index; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }

        private static uint CountLines(USpan<char> text)
        {
            uint lines = 1;
            for (uint i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lines++;
                }
            }

            return lines;
        }

        private static (uint start, uint length) GetLine(USpan<char> text, uint lineNumber)
        {
            uint line = 0;
            uint start = 0;
            for (uint i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    if (line == lineNumber)
                    {
                        return (start, i - start);
                    }

                    line++;
                    start = i + 1;
                }
            }

            return (start, text.Length - start);
        }

        private static void HandleCharacter(World world, Label textLabel, TextValidation validation, char character, PressedCharacters pressedCharacters, ref TextSelection selection)
        {
            USpan<char> text = textLabel.ProcessedText;
            Font font = textLabel.Font;
            Vector2 fontSize = textLabel.Size;
            uint start = Math.Min(selection.start, selection.end);
            uint end = Math.Max(selection.start, selection.end);
            uint length = end - start;
            bool shift = pressedCharacters.Contains(Settings.ShiftCharacter);
            bool groupSeparator = pressedCharacters.Contains(Settings.ControlCharacter);

            if (character == Settings.ControlCharacter || character == Settings.ShiftCharacter || character == Settings.EscapeCharacter)
            {
                //skip
            }
            else if (character == Settings.MoveUpCharacter)
            {
                uint lineNumber = GetLineNumber(text, selection.index);
                if (lineNumber > 0)
                {
                    (uint start, uint length) thisLine = GetLine(text, lineNumber);
                    (uint start, uint length) lineAbove = GetLine(text, lineNumber - 1);
                    if (shift && length == 0)
                    {
                        selection.start = selection.index;
                    }

                    uint localIndex = selection.index - thisLine.start;
                    selection.index = lineAbove.start + Math.Min(localIndex, lineAbove.length);

                    if (shift)
                    {
                        selection.end = selection.index;
                    }
                }

                if (!shift)
                {
                    selection.start = 0;
                    selection.end = 0;
                }
            }
            else if (character == Settings.MoveDownCharacter)
            {
                uint lineNumber = GetLineNumber(text, selection.index);
                uint lineCount = CountLines(text);
                if (lineNumber < lineCount - 1)
                {
                    (uint start, uint length) thisLine = GetLine(text, lineNumber);
                    (uint start, uint length) lineBelow = GetLine(text, lineNumber + 1);
                    if (shift && length == 0)
                    {
                        selection.start = selection.index;
                    }

                    uint localIndex = selection.index - thisLine.start;
                    selection.index = lineBelow.start + Math.Min(localIndex, lineBelow.length);

                    if (shift)
                    {
                        selection.end = selection.index;
                    }
                }

                if (!shift)
                {
                    selection.start = 0;
                    selection.end = 0;
                }
            }
            else if (character == Settings.MoveLeftCharacter)
            {
                if (selection.index > 0)
                {
                    if (groupSeparator)
                    {
                        if (shift && length == 0)
                        {
                            selection.start = selection.index;
                        }

                        if (TryGetPreviousIndex(text.Slice(0, selection.index - 1), out uint index))
                        {
                            selection.index = index + 1;
                        }
                        else
                        {
                            selection.index = 0;
                        }

                        if (shift)
                        {
                            selection.end = selection.index;
                        }
                    }
                    else
                    {
                        if (shift && length == 0)
                        {
                            selection.start = selection.index;
                        }

                        selection.index--;

                        if (shift)
                        {
                            selection.end = selection.index;
                        }
                    }
                }

                if (!shift)
                {
                    selection.start = 0;
                    selection.end = 0;
                }
            }
            else if (character == Settings.MoveRightCharacter)
            {
                if (selection.index < text.Length)
                {
                    if (groupSeparator)
                    {
                        if (shift && length == 0)
                        {
                            selection.start = selection.index;
                        }

                        if (TryGetNextIndex(text.Slice(selection.index + 1), out uint index))
                        {
                            selection.index += index + 1;
                        }
                        else
                        {
                            selection.index = text.Length;
                        }

                        if (shift)
                        {
                            selection.end = selection.index;
                        }
                    }
                    else
                    {
                        if (shift && length == 0)
                        {
                            selection.start = selection.index;
                        }

                        selection.index++;

                        if (shift)
                        {
                            selection.end = selection.index;
                        }
                    }
                }

                if (!shift)
                {
                    selection.start = 0;
                    selection.end = 0;
                }
            }
            else if (character == Settings.StartOfTextCharacter)
            {
                //move cursor to start
                if (shift && length == 0)
                {
                    selection.start = selection.index;
                }

                selection.index = 0;

                if (shift)
                {
                    selection.end = selection.index;
                }
                else
                {
                    selection.start = 0;
                    selection.end = 0;
                }
            }
            else if (character == Settings.EndOfTextCharacter)
            {
                //move cursor to end
                if (shift && length == 0)
                {
                    selection.start = selection.index;
                }

                selection.index = text.Length;

                if (shift)
                {
                    selection.end = selection.index;
                }
                else
                {
                    selection.start = 0;
                    selection.end = 0;
                }
            }
            else if (character == '\b')
            {
                //backspace
                if (text.Length == 0)
                {
                    return;
                }

                if (length > 0)
                {
                    RemoveSelection(textLabel, validation, ref selection);
                }
                else
                {
                    if (selection.index == 0)
                    {
                        return;
                    }

                    if (selection.index == text.Length)
                    {
                        //remove last char
                        USpan<char> newText = stackalloc char[(int)(text.Length - 1)];
                        text.Slice(0, text.Length - 1).CopyTo(newText);
                        SetText(textLabel, text, newText, validation);
                    }
                    else if (text.Length == 1)
                    {
                        SetText(textLabel, text, "".AsSpan(), validation);
                    }
                    else
                    {
                        //remove char at cursor
                        USpan<char> newText = stackalloc char[(int)(text.Length - 1)];
                        //copy first part
                        text.Slice(0, selection.index - 1).CopyTo(newText);
                        //copy remaining
                        text.Slice(selection.index).CopyTo(newText.Slice(selection.index - 1));
                        SetText(textLabel, text, newText, validation);
                    }

                    selection.index--;
                }
            }
            else
            {
                if (length > 0)
                {
                    RemoveSelection(textLabel, validation, ref selection);
                    text = textLabel.ProcessedText;
                }

                //write char
                bool holdingShift = pressedCharacters.Contains(Settings.ShiftCharacter);
                if (holdingShift)
                {
                    if (character == '1')
                    {
                        character = '!';
                    }
                    else if (character == '2')
                    {
                        character = '@';
                    }
                    else if (character == '3')
                    {
                        character = '#';
                    }
                    else if (character == '4')
                    {
                        character = '$';
                    }
                    else if (character == '5')
                    {
                        character = '%';
                    }
                    else if (character == '6')
                    {
                        character = '^';
                    }
                    else if (character == '7')
                    {
                        character = '&';
                    }
                    else if (character == '8')
                    {
                        character = '*';
                    }
                    else if (character == '9')
                    {
                        character = '(';
                    }
                    else if (character == '0')
                    {
                        character = ')';
                    }
                    else if (character == '-')
                    {
                        character = '_';
                    }
                    else if (character == '=')
                    {
                        character = '+';
                    }
                    else if (character == '[')
                    {
                        character = '{';
                    }
                    else if (character == ']')
                    {
                        character = '}';
                    }
                    else if (character == '\\')
                    {
                        character = '|';
                    }
                    else if (character == ';')
                    {
                        character = ':';
                    }
                    else if (character == '\'')
                    {
                        character = '"';
                    }
                    else if (character == ',')
                    {
                        character = '<';
                    }
                    else if (character == '.')
                    {
                        character = '>';
                    }
                    else if (character == '/')
                    {
                        character = '?';
                    }
                    else if (character == '`')
                    {
                        character = '~';
                    }
                    else
                    {
                        character = char.ToUpperInvariant(character);
                    }
                }

                //insert character into cursor position
                USpan<char> newText = stackalloc char[(int)(text.Length + 1)];
                uint index = Math.Min(selection.index, text.Length);
                USpan<char> firstPart = text.Slice(0, index);
                firstPart.CopyTo(newText);
                newText[index] = character;
                if (index + 1 < newText.Length)
                {
                    USpan<char> secondPart = text.Slice(index);
                    secondPart.CopyTo(newText.Slice(index + 1));
                }

                if (validation != default)
                {
                    using Text newTextContainer = new(newText);
                    validation.Invoke(text, newTextContainer);
                    textLabel.SetText(newTextContainer.AsSpan());
                    selection.index = newTextContainer.Length;
                }
                else
                {
                    textLabel.SetText(newText);
                    selection.index++;
                }
            }
        }

        private static void RemoveSelection(Label textLabel, TextValidation validation, ref TextSelection range)
        {
            uint start = Math.Min(range.start, range.end);
            uint end = Math.Max(range.start, range.end);
            uint length = end - start;
            USpan<char> text = textLabel.ProcessedText;
            USpan<char> newText = stackalloc char[(int)(text.Length - length)];

            if (start > 0)
            {
                text.Slice(0, start).CopyTo(newText);
            }

            if (end < text.Length)
            {
                USpan<char> endText = text.Slice(end);
                endText.CopyTo(newText.Slice(start));
            }

            SetText(textLabel, text, newText, validation);
            range.start = 0;
            range.end = 0;
            range.index = start;
        }

        private static void SetText(Label label, USpan<char> oldText, USpan<char> newText, TextValidation validation)
        {
            if (validation != default)
            {
                using Text newTextContainer = new(newText);
                validation.Invoke(oldText, newTextContainer);
                label.SetText(newTextContainer.AsSpan());
            }
            else
            {
                label.SetText(newText);
            }
        }
    }
}