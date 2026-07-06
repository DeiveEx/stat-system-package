using NUnit.Framework;
using UnityEngine;

namespace DeiveEx.StatSystem.EditorTests
{
	[Category("StatsSystem")]
	public class StatSystemTests
	{
		private enum TestStat
		{
			A,
			B,
		}

		private StatsContainer<TestStat> _statsContainer;

		//Setup methods are executed before each test
		[SetUp]
		public void Setup()
		{
			_statsContainer = new StatsContainer<TestStat>(new DefaultStatResolver<TestStat>(), "testID");
			_statsContainer.AddStat(TestStat.A, 100);
		}

		//TearDown methods are executed after each test
		[TearDown]
		public void TearDown() { }

		[Test]
		public void TestStat_was_created_correctly()
		{
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(100));
		}

		[Test]
		public void StatsContainer_Created_Correctly()
		{
			Assert.That(_statsContainer, Is.Not.Null);

			Assert.That(_statsContainer.HasStat(TestStat.A), Is.True);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(100));
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(100));

			Assert.That(_statsContainer.HasStat(TestStat.B), Is.False);
		}

		[TestCase(0)]
		[TestCase(5)]
		[TestCase(-10)]
		public void Stat_Value_is_correctly_changed(float value)
		{
			_statsContainer.SetStat(TestStat.A, value);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(value));
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(value));
		}

		[Test]
		public void Is_BaseValue_and_Value_Equal_Without_Modifiers()
		{
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(_statsContainer.GetStatBaseValue(TestStat.A)));
		}

		[Test]
		public void Stat_Is_Correctly_Removed()
		{
			int statRemoved = 0;
			_statsContainer.StatRemoved += (sender, e) => { statRemoved++; };

			Assert.That(_statsContainer.RemoveStat(TestStat.A), Is.True);
			Assert.That(_statsContainer.HasStat(TestStat.A), Is.False);
			Assert.That(statRemoved, Is.EqualTo(1));

			Assert.That(_statsContainer.RemoveStat(TestStat.A), Is.False);
			Assert.That(statRemoved, Is.EqualTo(1));
		}

		[Test]
		public void RemoveStat_Also_Removes_BaseValue_Handler()
		{
			_statsContainer.RegisterBaseValueHandler(TestStat.A, (stat, targetValue, container) => Mathf.Max(targetValue, 0));
			_statsContainer.RemoveStat(TestStat.A);

			//If the handler was correctly removed, re-adding the stat should allow negative values again
			_statsContainer.AddStat(TestStat.A);
			_statsContainer.SetStat(TestStat.A, -10);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(-10));
		}

		[Test]
		public void Removing_Unknown_Modifier_Returns_False()
		{
			Assert.That(_statsContainer.RemoveModifier(TestStat.A, "doesNotExist"), Is.False);
			Assert.That(_statsContainer.RemoveModifier(TestStat.A, "doesNotExist", true), Is.False);
		}

		[Test]
		public void AddToStat_Only_Affects_BaseValue_When_Modifiers_Are_Active()
		{
			_statsContainer.ApplyModifier(TestStat.A, new AdditiveModifier("mod", 50));
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(150));

			_statsContainer.AddToStat(TestStat.A, 10);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(110));
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(160));

			//Removing the modifier should leave only the base value, without the modifier baked in
			_statsContainer.RemoveModifier(TestStat.A, "mod");
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(110));
		}

		[Test]
		public void SetOrAddStat_Creates_Or_Sets()
		{
			int statAdded = 0;
			int baseChanged = 0;
			_statsContainer.StatAdded += (sender, e) => { statAdded++; };
			_statsContainer.StatBaseValueChanged += (sender, e) => { baseChanged++; };

			//Stat doesn't exist yet: should be created with the value, without firing a value change
			_statsContainer.SetOrAddStat(TestStat.B, 10);
			Assert.That(_statsContainer.GetStat(TestStat.B), Is.EqualTo(10));
			Assert.That(statAdded, Is.EqualTo(1));
			Assert.That(baseChanged, Is.EqualTo(0));

			//Stat exists: should just be set
			_statsContainer.SetOrAddStat(TestStat.B, 20);
			Assert.That(_statsContainer.GetStat(TestStat.B), Is.EqualTo(20));
			Assert.That(statAdded, Is.EqualTo(1));
			Assert.That(baseChanged, Is.EqualTo(1));
		}

		[Test]
		public void Events_are_Fired_Correctly()
		{
			int statAdded = 0;
			int baseChanged = 0;
			int currentChanged = 0;
			int modifierChanged = 0;

			_statsContainer.StatAdded += (sender, e) => { statAdded++; };
			_statsContainer.StatBaseValueChanged += (sender, e) => { baseChanged++; };
			_statsContainer.StatValueChanged += (sender, e) => { currentChanged++; };
			_statsContainer.ModifierAdded += (sender, e) => { modifierChanged++; };
			_statsContainer.ModifierRemoved += (sender, e) => { modifierChanged--; };

			_statsContainer.AddStat(TestStat.B);
			Assert.That(statAdded, Is.EqualTo(1));

			_statsContainer.SetStat(TestStat.A, 1);
			Assert.That(baseChanged, Is.EqualTo(1));
			Assert.That(currentChanged, Is.EqualTo(1));
			Assert.That(modifierChanged, Is.EqualTo(0));

			//Modifiers changing the current value should also fire StatValueChanged, but not StatBaseValueChanged
			StatModifier modifier = new AdditiveModifier("test", 10);
			_statsContainer.ApplyModifier(TestStat.A, modifier);

			Assert.That(modifierChanged, Is.EqualTo(1));
			Assert.That(currentChanged, Is.EqualTo(2));
			Assert.That(baseChanged, Is.EqualTo(1));

			_statsContainer.RemoveModifier(TestStat.A, modifier.ID);
			Assert.That(modifierChanged, Is.EqualTo(0));
			Assert.That(currentChanged, Is.EqualTo(3));
		}

		[Test]
		public void Events_are_Not_Fired_When_Value_Does_Not_Change()
		{
			int baseChanged = 0;
			int currentChanged = 0;

			_statsContainer.StatBaseValueChanged += (sender, e) => { baseChanged++; };
			_statsContainer.StatValueChanged += (sender, e) => { currentChanged++; };

			//Setting the same value should not fire any change events
			_statsContainer.SetStat(TestStat.A, _statsContainer.GetStatBaseValue(TestStat.A));
			Assert.That(baseChanged, Is.EqualTo(0));
			Assert.That(currentChanged, Is.EqualTo(0));

			//A modifier with no effect should not fire StatValueChanged
			_statsContainer.ApplyModifier(TestStat.A, new AdditiveModifier("noop", 0));
			Assert.That(currentChanged, Is.EqualTo(0));
		}

		[TestCase(10)]
		[TestCase(1000)]
		[TestCase(0)]
		[TestCase(-10)]
		public void Stat_BaseValue_Handlers_Are_Correctly_Registered_And_Executed(float value)
		{
			_statsContainer.RegisterBaseValueHandler(TestStat.A, (stat, targetValue, container) => Mathf.Max(targetValue, 0));
			_statsContainer.SetStat(TestStat.A, value);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(Mathf.Max(value, 0)));

			_statsContainer.UnregisterBaseValueHandler(TestStat.A);
			_statsContainer.SetStat(TestStat.A, value);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(value));
		}

		[Test]
		public void Registering_A_BaseValue_Handler_Replaces_The_Previous_One()
		{
			_statsContainer.RegisterBaseValueHandler(TestStat.A, value => Mathf.Max(value, 0));
			Assert.DoesNotThrow(() => _statsContainer.RegisterBaseValueHandler(TestStat.A, value => Mathf.Max(value, 10)));

			_statsContainer.SetStat(TestStat.A, -5);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(10));
		}

		[Test]
		public void Modifier_Events_Carry_The_Modifier()
		{
			ModifierChangedEventArgs<TestStat> addedArgs = null;
			ModifierChangedEventArgs<TestStat> removedArgs = null;

			_statsContainer.ModifierAdded += (sender, e) => { addedArgs = e; };
			_statsContainer.ModifierRemoved += (sender, e) => { removedArgs = e; };

			var modifier = new AdditiveModifier("test", 10);
			_statsContainer.ApplyModifier(TestStat.A, modifier);

			Assert.That(addedArgs, Is.Not.Null);
			Assert.That(addedArgs.Stat, Is.EqualTo(TestStat.A));
			Assert.That(addedArgs.Modifier, Is.SameAs(modifier));

			_statsContainer.RemoveModifier(TestStat.A, modifier.ID);

			Assert.That(removedArgs, Is.Not.Null);
			Assert.That(removedArgs.Modifier, Is.SameAs(modifier));
		}

		[Test]
		public void Modifier_Handle_Removes_The_Modifier_When_Disposed()
		{
			var handle = _statsContainer.ApplyModifier(TestStat.A, new AdditiveModifier("test", 10));
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(110));

			handle.Dispose();
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(100));

			//Disposing again should be a safe no-op, even if another modifier with the same ID was applied since
			_statsContainer.ApplyModifier(TestStat.A, new AdditiveModifier("test", 10));
			handle.Dispose();
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(110));
		}

		[Test]
		public void Modifier_Handle_Is_Safe_To_Dispose_After_Stat_Removal()
		{
			var handle = _statsContainer.ApplyModifier(TestStat.A, new AdditiveModifier("test", 10));
			_statsContainer.RemoveStat(TestStat.A);

			Assert.DoesNotThrow(() => handle.Dispose());
		}

		[Test]
		public void Modifier_Is_Removed_By_Instance()
		{
			var first = new AdditiveModifier("test", 10);
			var second = new AdditiveModifier("test", 20);
			_statsContainer.ApplyModifier(TestStat.A, first);
			_statsContainer.ApplyModifier(TestStat.A, second);

			//Both share the same ID, but only the exact instance should be removed
			Assert.That(_statsContainer.RemoveModifier(TestStat.A, second), Is.True);
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(110));

			Assert.That(_statsContainer.RemoveModifier(TestStat.A, second), Is.False);
		}

		[Test]
		public void HasModifier_And_ModifierCount_Are_Correct()
		{
			Assert.That(_statsContainer.HasModifier(TestStat.A, "test"), Is.False);
			Assert.That(_statsContainer.GetStatModifiers(TestStat.A).Count, Is.EqualTo(0));

			_statsContainer.ApplyModifier(TestStat.A, new AdditiveModifier("test", 10));
			_statsContainer.ApplyModifier(TestStat.A, new MultiplicativeModifier("other", 1));

			Assert.That(_statsContainer.HasModifier(TestStat.A, "test"), Is.True);
			Assert.That(_statsContainer.GetStatModifiers(TestStat.A).Count, Is.EqualTo(2));
		}

		[Test]
		public void RecalculateStat_Refreshes_Cross_Stat_Modifiers()
		{
			//A's current value is derived from B's base value
			_statsContainer.AddStat(TestStat.B, 10);
			_statsContainer.ApplyModifier(TestStat.A, new CustomCalculationModifier("dependsOnB",
				(baseValue, currentValue) => currentValue + _statsContainer.GetStatBaseValue(TestStat.B)));

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(110));

			//Changing B doesn't automatically refresh A...
			_statsContainer.SetStat(TestStat.B, 50);
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(110));

			//...until we recalculate it
			_statsContainer.RecalculateStat(TestStat.A);
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(150));
		}

		[Test]
		public void Snapshot_Restores_Base_Values()
		{
			_statsContainer.ApplyModifier(TestStat.A, new AdditiveModifier("buff", 50));
			var snapshot = _statsContainer.GetBaseValueSnapshot();

			_statsContainer.SetStat(TestStat.A, 5);
			_statsContainer.ApplySnapshot(snapshot);

			//Base values are restored; modifiers are untouched
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(100));
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(150));

			//Applying to a container that doesn't have the stat creates it
			var otherContainer = new StatsContainer<TestStat>(new DefaultStatResolver<TestStat>());
			otherContainer.ApplySnapshot(snapshot);
			Assert.That(otherContainer.GetStat(TestStat.A), Is.EqualTo(100));
		}
	}
}