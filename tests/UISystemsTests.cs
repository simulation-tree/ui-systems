using Automations;
using Cameras;
using Data;
using Fonts;
using Materials;
using Meshes;
using Rendering;
using Simulation.Tests;
using TextRendering;
using Textures;
using Transforms;
using Types;
using UI.Messages;
using Worlds;

namespace UI.Systems.Tests
{
    public abstract class UISystemsTests : SimulationTests
    {
        public World world;

        static UISystemsTests()
        {
            MetadataRegistry.Load<RenderingMetadataBank>();
            MetadataRegistry.Load<MaterialsMetadataBank>();
            MetadataRegistry.Load<UIMetadataBank>();
            MetadataRegistry.Load<AutomationsMetadataBank>();
            MetadataRegistry.Load<TransformsMetadataBank>();
            MetadataRegistry.Load<MeshesMetadataBank>();
            MetadataRegistry.Load<DataMetadataBank>();
            MetadataRegistry.Load<FontsMetadataBank>();
            MetadataRegistry.Load<TexturesMetadataBank>();
            MetadataRegistry.Load<CamerasMetadataBank>();
            MetadataRegistry.Load<TextRenderingMetadataBank>();
            MetadataRegistry.Load<UISystemsTestsMetadataBank>();
        }

        protected override void SetUp()
        {
            base.SetUp();
            Schema schema = new();
            schema.Load<RenderingSchemaBank>();
            schema.Load<MaterialsSchemaBank>();
            schema.Load<UISchemaBank>();
            schema.Load<AutomationsSchemaBank>();
            schema.Load<TransformsSchemaBank>();
            schema.Load<MeshesSchemaBank>();
            schema.Load<DataSchemaBank>();
            schema.Load<FontsSchemaBank>();
            schema.Load<TexturesSchemaBank>();
            schema.Load<CamerasSchemaBank>();
            schema.Load<TextRenderingSchemaBank>();
            schema.Load<UISystemsTestsSchemaBank>();
            world = new(schema);
        }

        protected override void TearDown()
        {
            world.Dispose();
            base.TearDown();
        }

        protected override void Update(double deltaTime)
        {
            Simulator.Broadcast(new UIUpdate());
        }
    }
}
