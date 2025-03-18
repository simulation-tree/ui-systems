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
using UI.Systems;
using Worlds;

namespace UI.Systems.Tests
{
    public abstract class UISystemsTests : SimulationTests
    {
        static UISystemsTests()
        {
            TypeRegistry.Load<RenderingTypeBank>();
            TypeRegistry.Load<MaterialsTypeBank>();
            TypeRegistry.Load<UITypeBank>();
            TypeRegistry.Load<AutomationsTypeBank>();
            TypeRegistry.Load<TransformsTypeBank>();
            TypeRegistry.Load<MeshesTypeBank>();
            TypeRegistry.Load<DataTypeBank>();
            TypeRegistry.Load<FontsTypeBank>();
            TypeRegistry.Load<TexturesTypeBank>();
            TypeRegistry.Load<CamerasTypeBank>();
            TypeRegistry.Load<TextRenderingTypeBank>();
            TypeRegistry.Load<UISystemsTestsTypeBank>();
        }
        protected override void SetUp()
        {
            base.SetUp();
            simulator.AddSystem(new ComponentMixingSystem());
            simulator.AddSystem(new InvokeTriggersSystem());
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
