using Cameras;
using Rendering;
using System;
using System.Runtime.InteropServices;
using UI.Components;
using UI.Functions;
using UI.Systems;
using UI.Systems.Tests;
using Unmanaged;
using Worlds;

namespace UI.Tests
{
    public unsafe class TextProcessingTests : UISystemsTests
    {
        private const string NounToken = "mushroom";
        private const string VerbToken = "goomba";
        private const string ReplacementNoun = "Yes we all know its a mushroom";
        private const string ReplacementVerb = "done";

        protected override void SetUp()
        {
            base.SetUp();
            Simulator.Add(new LabelTextSystem(Simulator, world));
        }

        protected override void TearDown()
        {
            Simulator.Remove<LabelTextSystem>();
            base.TearDown();
        }

        [Test]
        public void SimpleKeywordReplacement()
        {
            Settings settings = new(world);
            Destination destination = new(world, new(1920, 1080), "dummy");
            Camera camera = Camera.CreateOrthographic(world, destination, 1f);
            Canvas canvas = new(settings, camera);

            string inputText = "The following text: `{{" + NounToken + "}}` is expected to be {{" + VerbToken + "}}";
            Label label = new(canvas, inputText);
            LabelProcessor.Create(world, new(&ReplaceNoun));
            LabelProcessor.Create(world, new(&ReplaceVerb));

            Update();

            Assert.That(label.ProcessedText.ToString(), Is.EqualTo($"The following text: `{ReplacementNoun}` is expected to be {ReplacementVerb}"));

            label.SetText("1: {{" + VerbToken + "}} 2: {{" + NounToken + "}}");

            Update();

            Assert.That(label.ProcessedText.ToString(), Is.EqualTo($"1: {ReplacementVerb} 2: {ReplacementNoun}"));
        }

        [UnmanagedCallersOnly]
        private static Bool ReplaceNoun(TryProcessLabel.Input input)
        {
            if (input.OriginalText.IndexOf(NounToken) != -1)
            {
                Span<char> newText = stackalloc char[input.OriginalText.Length + 32];
                int newLength = Text.Replace(input.OriginalText, "{{" + NounToken + "}}", ReplacementNoun, newText);
                input.SetResult(newText.Slice(0, newLength));
                return true;
            }

            return false;
        }

        [UnmanagedCallersOnly]
        private static Bool ReplaceVerb(TryProcessLabel.Input input)
        {
            if (input.OriginalText.IndexOf(VerbToken) != -1)
            {
                Span<char> newText = stackalloc char[input.OriginalText.Length + 32];
                int newLength = Text.Replace(input.OriginalText, "{{" + VerbToken + "}}", ReplacementVerb, newText);
                input.SetResult(newText.Slice(0, newLength));
                return true;
            }

            return false;
        }

        [Test]
        public void UseTokenEntities()
        {
            Settings settings = new(world);
            Destination destination = new(world, new(1920, 1080), "dummy");
            Camera camera = Camera.CreateOrthographic(world, destination, 1f);
            Canvas canvas = new(settings, camera);

            string inputText = "The following text: `{{" + NounToken + ":System.Char[]}}` is expected to be {{" + VerbToken + ":System.Char[]}}";
            Label label = new(canvas, inputText);
            Entity nounEntity = new(world);
            nounEntity.CreateArray(ReplacementNoun.AsSpan());
            nounEntity.AddComponent(new IsToken(NounToken));
            Entity verbEntity = new(world);
            verbEntity.CreateArray(ReplacementVerb.AsSpan());
            verbEntity.AddComponent(new IsToken(VerbToken));

            Update();

            Assert.That(label.ProcessedText.ToString(), Is.EqualTo($"The following text: `{ReplacementNoun}` is expected to be {ReplacementVerb}"));

            label.SetText("1: {{" + VerbToken + ":System.Char[]}} 2: {{" + NounToken + ":System.Char[]}}");

            Update();

            Assert.That(label.ProcessedText.ToString(), Is.EqualTo($"1: {ReplacementVerb} 2: {ReplacementNoun}"));
        }
    }
}