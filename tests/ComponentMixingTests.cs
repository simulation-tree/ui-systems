using System;
using System.Numerics;
using UI.Components;
using Worlds;

namespace UI.Systems.Tests
{
    public class ComponentMixingTests : UISystemsTests
    {
        [Test]
        public void IntegerAddition()
        {
            uint entity = world.CreateEntity();
            world.AddComponent(entity, new First(7));
            world.AddComponent(entity, new Second(9123));
            world.AddComponent(entity, ComponentMix.Create<First, Second, Result>(ComponentMix.Operation.UnsignedAdd, 1, world.Schema));

            simulator.Update(TimeSpan.FromSeconds(0.1f));

            Assert.That(world.ContainsComponent<Result>(entity), Is.True);
            int first = world.GetComponent<First>(entity).value;
            int second = world.GetComponent<Second>(entity).value;
            int result = world.GetComponent<Result>(entity).value;
            Assert.That(result, Is.EqualTo(first + second));

            ref ComponentMix mix = ref world.GetComponent<ComponentMix>(entity);
            mix.operation = ComponentMix.Operation.SignedAdd;
            world.SetComponent(entity, new First(-424));

            simulator.Update(TimeSpan.FromSeconds(0.1f));

            first = world.GetComponent<First>(entity).value;
            second = world.GetComponent<Second>(entity).value;
            result = world.GetComponent<Result>(entity).value;
            Assert.That(result, Is.EqualTo(first + second));
        }

        [Test]
        public void FloatingMultiply()
        {
            uint entity = world.CreateEntity();
            world.AddComponent(entity, new FirstFloat(7.5f));
            world.AddComponent(entity, new SecondFloat(0.5f));
            world.AddComponent(entity, ComponentMix.Create<FirstFloat, SecondFloat, ResultFloat>(ComponentMix.Operation.FloatingMultiply, 1, world.Schema));

            simulator.Update(TimeSpan.FromSeconds(0.1f));

            Assert.That(world.ContainsComponent<ResultFloat>(entity), Is.True);
            float first = world.GetComponent<FirstFloat>(entity).value;
            float second = world.GetComponent<SecondFloat>(entity).value;
            float result = world.GetComponent<ResultFloat>(entity).value;
            Assert.That(result, Is.EqualTo(first * second));
            ref ComponentMix mix = ref world.GetComponent<ComponentMix>(entity);
            mix.operation = ComponentMix.Operation.FloatingAdd;
            world.SetComponent(entity, new FirstFloat(-424.5f));

            simulator.Update(TimeSpan.FromSeconds(0.1f));

            first = world.GetComponent<FirstFloat>(entity).value;
            second = world.GetComponent<SecondFloat>(entity).value;
            result = world.GetComponent<ResultFloat>(entity).value;
            Assert.That(result, Is.EqualTo(first + second));
        }

        [Test]
        public void MixTwoVectors()
        {
            uint entity = world.CreateEntity();
            world.AddComponent(entity, new FirstVector(new Vector3(1f, 2f, 3f)));
            world.AddComponent(entity, new SecondVector(new Vector3(0.5f, 0.5f, 0.5f)));
            world.AddComponent(entity, ComponentMix.Create<FirstVector, SecondVector, ResultVector>(ComponentMix.Operation.FloatingMultiply, 3, world.Schema));

            simulator.Update(TimeSpan.FromSeconds(0.1f));

            Assert.That(world.ContainsComponent<ResultVector>(entity), Is.True);
            Vector3 first = world.GetComponent<FirstVector>(entity).value;
            Vector3 second = world.GetComponent<SecondVector>(entity).value;
            Vector3 result = world.GetComponent<ResultVector>(entity).value;
            Assert.That(result, Is.EqualTo(first * second));
        }
    }

    [Component]
    public struct Result
    {
        public int value;

        public Result(int value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct First
    {
        public int value;

        public First(int value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct Second
    {
        public int value;

        public Second(int value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct FirstFloat
    {
        public float value;

        public FirstFloat(float value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct SecondFloat
    {
        public float value;

        public SecondFloat(float value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct ResultFloat
    {
        public float value;

        public ResultFloat(float value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct FirstVector
    {
        public Vector3 value;

        public FirstVector(Vector3 value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct SecondVector
    {
        public Vector3 value;

        public SecondVector(Vector3 value)
        {
            this.value = value;
        }
    }

    [Component]
    public struct ResultVector
    {
        public Vector3 value;

        public ResultVector(Vector3 value)
        {
            this.value = value;
        }
    }
}
