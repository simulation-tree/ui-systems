using System.Runtime.InteropServices;
using UI.Components;
using UI.Functions;
using Worlds;

namespace UI.Systems.Tests
{
    public class TriggerTests : UISystemsTests
    {
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

            Simulator.Update(0.1);

            Assert.That(world.ContainsComponent<byte>(triggerA), Is.True);
            Assert.That(world.ContainsComponent<byte>(triggerB), Is.True);
            Assert.That(world.ContainsComponent<int>(triggerC), Is.True);

            world.GetComponent<IsTrigger>(triggerA).filter = new(&SelectFirstEntity);
            world.GetComponent<IsTrigger>(triggerB).filter = new(&SelectFirstEntity);
            world.GetComponent<IsTrigger>(triggerC).filter = new(&SelectFirstEntity);

            Simulator.Update(0.1);

            Assert.That(world.ContainsComponent<byte>(triggerA), Is.False);
            Assert.That(world.ContainsComponent<byte>(triggerB), Is.True);

            [UnmanagedCallersOnly]
            static void FilterEverythingOut(TriggerFilter.Input input)
            {
                foreach (ref Entity entity in input.Entities)
                {
                    entity = default;
                }
            }

            [UnmanagedCallersOnly]
            static void SelectFirstEntity(TriggerFilter.Input input)
            {
                foreach (ref Entity entity in input.Entities)
                {
                    if (entity.value != 1)
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