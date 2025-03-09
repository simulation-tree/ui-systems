using Cameras;
using Rendering;
using System;
using System.Runtime.InteropServices;
using UI.Functions;
using UI.Systems;
using UI.Systems.Tests;
using Unmanaged;

namespace UI.Tests
{
    public unsafe class TextProcessingTests : UISystemsTests
    {
        private const string NounToken = "{{mushroom}}";
        private const string VerbToken = "{{verb}}";
        private const string ReplacementNoun = "Yes we all know its a mushroom";
        private const string ReplacementVerb = "done";

        [Test]
        public void SimpleKeywordReplacement()
        {
            simulator.AddSystem<LabelTextSystem>();

            Settings settings = new(world);
            Destination destination = new(world, new(1920, 1080), "dummy");
            Camera camera = Camera.CreateOrthographic(world, destination, 1f);
            Canvas canvas = new(settings, camera);

            string inputText = $"The following text: `{NounToken}` is expected to be {VerbToken}";
            Label label = new(canvas, inputText);
            new LabelProcessor(world, new(&ReplaceNoun));
            new LabelProcessor(world, new(&ReplaceVerb));

            simulator.Update();

            Assert.That(label.ProcessedText.ToString(), Is.EqualTo($"The following text: `{ReplacementNoun}` is expected to be {ReplacementVerb}"));

            label.SetText($"1: {VerbToken} 2: {NounToken}");

            simulator.Update();

            Assert.That(label.ProcessedText.ToString(), Is.EqualTo($"1: {ReplacementVerb} 2: {ReplacementNoun}"));

            simulator.RemoveSystem<LabelTextSystem>();
        }

        [UnmanagedCallersOnly]
        private static Boolean ReplaceNoun(TryProcessLabel.Input input)
        {
            if (input.OriginalText.IndexOf(NounToken) != -1)
            {
                Span<char> newText = stackalloc char[input.OriginalText.Length + 32];
                int newLength = Text.Replace(input.OriginalText, NounToken, ReplacementNoun, newText);
                input.SetResult(newText.Slice(0, newLength));
                return true;
            }

            return false;
        }

        [UnmanagedCallersOnly]
        private static Boolean ReplaceVerb(TryProcessLabel.Input input)
        {
            if (input.OriginalText.IndexOf(VerbToken) != -1)
            {
                Span<char> newText = stackalloc char[input.OriginalText.Length + 32];
                int newLength = Text.Replace(input.OriginalText, VerbToken, ReplacementVerb, newText);
                input.SetResult(newText.Slice(0, newLength));
                return true;
            }

            return false;
        }
    }
}