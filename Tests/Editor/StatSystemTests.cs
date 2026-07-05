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

			Assert.That(_statsContainer.StatExists(TestStat.A), Is.True);
			Assert.That(_statsContainer.GetStatBaseValue(TestStat.A), Is.EqualTo(100));
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(100));

			Assert.That(_statsContainer.StatExists(TestStat.B), Is.False);
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
			Assert.That(_statsContainer.StatExists(TestStat.A), Is.False);
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
	}
}