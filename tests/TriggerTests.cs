using System.Runtime.InteropServices;
using UI.Components;
using UI.Functions;
using Worlds;

namespace UI.Systems.Tests
{
    public class TriggerTests : UISystemsTests
    {
        protected override void SetUp()
        {
            base.SetUp();
            Simulator.Add(new InvokeTriggersSystem(Simulator, world));
        }

        protected override void TearDown()
        {
            Simulator.Remove<InvokeTriggersSystem>();
            base.TearDown();
        }

        [Test]
        public unsafe void CheckTrigger()
        {
            uint triggerA = world.CreateEntity();
            uint triggerB = world.CreateEntity();
            uint triggerC = world.CreateEntity();
            world.AddComponent(triggerA, new IsTrigger(new(&FilterEverythingOut), new(&RemoveByteComponent)));
            world.AddComponent(triggerB, new IsTrigger(new(&FilterEverythingOut), new(&RemoveByteComponent)));
            world.AddComponent(triggerC, new IsTrigger(new(&FilterEverythingOut), new(&RemoveByteComponent)));

            world.AddComponent(triggerA, (byte)1);
            world.AddComponent(triggerB, (byte)2);
            world.AddComponent(triggerC, 3);

            Update(0.1);

            Assert.That(world.ContainsComponent<byte>(triggerA), Is.True);
            Assert.That(world.ContainsComponent<byte>(triggerB), Is.True);
            Assert.That(world.ContainsComponent<byte>(triggerC), Is.False);
            Assert.That(world.ContainsComponent<int>(triggerC), Is.True);

            world.GetComponent<IsTrigger>(triggerA).filter = new(&SelectFirstEntity);
            world.GetComponent<IsTrigger>(triggerB).filter = new(&SelectFirstEntity);
            world.GetComponent<IsTrigger>(triggerC).filter = new(&SelectFirstEntity);

            Update(0.1);

            Assert.That(world.ContainsComponent<byte>(triggerA), Is.False);
            Assert.That(world.ContainsComponent<byte>(triggerB), Is.True);
            Assert.That(world.ContainsComponent<byte>(triggerC), Is.False);
            Assert.That(world.ContainsComponent<int>(triggerC), Is.True);

            [UnmanagedCallersOnly]
            static void FilterEverythingOut(TriggerFilter.Input input)
            {
                foreach (ref uint entity in input.Entities)
                {
                    entity = default;
                }
            }

            [UnmanagedCallersOnly]
            static void SelectFirstEntity(TriggerFilter.Input input)
            {
                foreach (ref uint entity in input.Entities)
                {
                    if (entity != 1)
                    {
                        entity = default;
                    }
                }
            }

            [UnmanagedCallersOnly]
            static void RemoveByteComponent(Entity entity)
            {
                entity.RemoveComponent<byte>();
            }
        }
    }
}