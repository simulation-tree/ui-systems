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
using Worlds;

namespace UI.Systems.Tests
{
    public abstract class UISystemsTests : SimulationTests
    {
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
            simulator.Add(new ComponentMixingSystem());
            simulator.Add(new InvokeTriggersSystem());
        }

        protected override void TearDown()
        {
            simulator.Remove<InvokeTriggersSystem>();
            simulator.Remove<ComponentMixingSystem>();
            base.TearDown();
        }

        protected override Schema CreateSchema()
        {
            Schema schema = base.CreateSchema();
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
            return schema;
        }
    }
}
