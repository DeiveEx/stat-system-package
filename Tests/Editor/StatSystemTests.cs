using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeiveEx.StatSystem.EditorTests
{
	[Category("StatsSystem")]

	public class StatSystemTests
	{
		private Stat _testStat;
		private StatsContainer _statsContainer;

		private const string STAT_NAME = "testStat";

		//Setup methods are executed before each test
		[SetUp]
		public void Setup()
		{
			_testStat = new Stat(STAT_NAME, 100);

			var initialStats = new List<Stat>()
			{
				_testStat
			};

			_statsContainer = new StatsContainer("testID");

			foreach (var stat in initialStats)
			{
				_statsContainer.AddStat(stat);
			}
		}

		//TearDown methods are executed after each test
		[TearDown]
		public void TearDown() { }

		[Test]
		public void TestStat_was_created_correctly()
		{
			Assert.AreEqual(_testStat.BaseValue, 100);
		}

		[Test]
		public void StatsContainer_Created_Correctly()
		{
			Assert.IsNotNull(_statsContainer);
			Assert.IsNotNull(_statsContainer.StatExists(STAT_NAME));
			Assert.AreEqual(_statsContainer.GetStatBaseValue(STAT_NAME), 100);
			Assert.AreEqual(_statsContainer.GetStatCurrentValue(STAT_NAME), 100);
		}

		[Test]
		[TestCase(5)]
		[TestCase(-10)]
		public void Stat_Value_is_correctly_changed(float value)
		{
			_testStat.BaseValue = value;
			Assert.AreEqual(_testStat.BaseValue, value);
		}

		[Test]
		public void Is_BaseValue_and_Value_Equal_Without_Modifiers()
		{
			Assert.AreEqual(_testStat.BaseValue, _testStat.CurrentValue);
		}

		[Test]
		[TestCase(5)]
		[TestCase(1000)]
		[TestCase(-10)]
		[TestCase(-1000)]
		[TestCase(10, 50)]
		[TestCase(10, 50, -30)]
		public void Is_Value_Correctly_Affected_By_Additive_Modifiers(params float[] magnitudes)
		{
			float initialValue = _testStat.CurrentValue;
			string id = "test";
			float total = initialValue;

			for (int i = 0; i < magnitudes.Length; i++)
			{
				StatModifier modifier = new StatModifier(id + i, OperationType.Additive, magnitudes[i]);
				_testStat.AddModifier(modifier);
				total += magnitudes[i];
			}

			Assert.AreEqual(_testStat.CurrentValue, total);

			for (int i = 0; i < magnitudes.Length; i++)
			{
				_testStat.RemoveModifier(id + i);
			}

			Assert.AreEqual(_testStat.CurrentValue, initialValue);
		}

		[Test]
		[TestCase(.1f)]
		[TestCase(-.1f)]
		[TestCase(1f)]
		[TestCase(-.5f)]
		[TestCase(.1f, -.5f)]
		[TestCase(.3f, .3f, -.1f)]
		public void Is_Value_Correctly_Affected_By_Multiplicative_Modifiers(params float[] magnitudes)
		{
			float initialValue = _testStat.CurrentValue;
			string id = "test";
			float total = 0;

			for (int i = 0; i < magnitudes.Length; i++)
			{
				StatModifier modifier = new StatModifier(id + i, OperationType.Multiplicative, magnitudes[i]);
				_testStat.AddModifier(modifier);
				total += magnitudes[i];
			}

			for (int i = 0; i < magnitudes.Length; i++)
			{
				_testStat.RemoveModifier(id + i);
			}

			Assert.AreEqual(_testStat.CurrentValue, initialValue);
		}

		[Test]
		[TestCase(1)]
		[TestCase(1000)]
		[TestCase(1000, 1)]
		[TestCase(1000, 50, 1)]
		public void Is_Value_Correctly_Affected_By_Override_Modifiers(params float[] magnitudesOrderedByPriority)
		{
			float initialValue = _testStat.CurrentValue;
			string id = "test";
			float magnitudeWithHighestPriority = magnitudesOrderedByPriority[magnitudesOrderedByPriority.Length - 1];

			for (int i = 0; i < magnitudesOrderedByPriority.Length; i++)
			{
				StatModifier modifier = new StatModifier(id + i, OperationType.Override, magnitudesOrderedByPriority[i], i);
				_testStat.AddModifier(modifier);
			}

			Assert.AreEqual(_testStat.CurrentValue, magnitudeWithHighestPriority);

			for (int i = 0; i < magnitudesOrderedByPriority.Length; i++)
			{
				_testStat.RemoveModifier(id + i);
			}

			Assert.AreEqual(_testStat.CurrentValue, initialValue);
		}

		[Test]
		[TestCase(10, .1f, 0)]
		[TestCase(0, -.5f, 0)]
		[TestCase(10, .5f, 1)]
		public void Is_Value_Correctly_Affected_By_Different_Modifiers(float additiveMagnitude, float multiplicativeMagnitude, float overrideMagnitude)
		{
			float initialValue = _testStat.CurrentValue;
			string id = "test";
			float expectedValue = 0;

			StatModifier addModifier = new StatModifier(id + 0, OperationType.Additive, additiveMagnitude);
			StatModifier multModifier = new StatModifier(id + 1, OperationType.Multiplicative, multiplicativeMagnitude);
			StatModifier overrideModifier = new StatModifier(id + 2, OperationType.Override, overrideMagnitude);

			_testStat.AddModifier(addModifier);
			expectedValue = initialValue + additiveMagnitude;
			Assert.AreEqual(_testStat.CurrentValue, expectedValue);

			_testStat.AddModifier(multModifier);
			expectedValue += (expectedValue * multiplicativeMagnitude);
			Assert.AreEqual(_testStat.CurrentValue, expectedValue);

			_testStat.AddModifier(overrideModifier);
			expectedValue = overrideMagnitude;
			Assert.AreEqual(_testStat.CurrentValue, expectedValue);

			for (int i = 0; i < 3; i++)
			{
				_testStat.RemoveModifier(id + i);
			}

			Assert.AreEqual(_testStat.CurrentValue, initialValue);
		}

		[Test]
		public void Custom_Modifier_Applied_Correctly()
		{
			StatModifier modifier = new StatModifier("test", OperationType.Custom, 0, 0, (baseValue, currentValue) => { return baseValue / 2f; });

			_testStat.AddModifier(modifier);
			Assert.AreEqual(_testStat.CurrentValue, _testStat.BaseValue / 2f);
		}

		[Test]
		public void Is_Single_Modifier_Correctly_Removed()
		{
			StatModifier modifier = new StatModifier("test", OperationType.Additive, 1);

			_testStat.AddModifier(modifier);
			_testStat.AddModifier(modifier);

			Assert.AreEqual(_testStat.CurrentValue, _testStat.BaseValue + 2);

			_testStat.RemoveModifier(modifier.id);

			Assert.AreEqual(_testStat.CurrentValue, _testStat.BaseValue + 1);
		}

		[Test]
		public void Is_Multiple_Modifiers_Correctly_Removed()
		{
			StatModifier modifier = new StatModifier("test", OperationType.Additive, 1);

			_testStat.AddModifier(modifier);
			_testStat.AddModifier(modifier);

			Assert.AreEqual(_testStat.CurrentValue, _testStat.BaseValue + 2);

			_testStat.RemoveModifier(modifier.id, true);

			Assert.AreEqual(_testStat.CurrentValue, _testStat.BaseValue);
		}

		[Test]
		public void Events_are_Fired_Correctly()
		{
			int baseChanged = 0;
			int modifierChanged = 0;

			_testStat.OnBaseValueChanged += (sender, e) => { baseChanged++; };

			_testStat.OnModifierAdded += (sender, e) => { modifierChanged++; };

			_testStat.OnModifierRemoved += (sender, e) => { modifierChanged--; };

			_testStat.BaseValue = 1;
			Assert.AreEqual(baseChanged, 1);
			Assert.AreEqual(modifierChanged, 0);

			StatModifier modifier = new StatModifier("test", OperationType.Additive, 10);
			_testStat.AddModifier(modifier);

			Assert.AreEqual(modifierChanged, 1);

			_testStat.RemoveModifier(modifier.id);
			Assert.AreEqual(modifierChanged, 0);
		}

		[Test]
		[TestCase(10)]
		[TestCase(1000)]
		[TestCase(0)]
		[TestCase(-10)]
		public void Stat_BaseValue_Handlers_Are_Correctly_Registered_And_Executed(float value)
		{
			StatBaseValueChangeHandler testHandler = new StatBaseValueChangeHandler(STAT_NAME, (targetState, targetValue, container) => { return Mathf.Max(targetValue, 0); });

			_statsContainer.RegisterBaseValueHandler(testHandler);
			_statsContainer.SetStatBaseValue(STAT_NAME, value);
			Assert.AreEqual(_statsContainer.GetStatBaseValue(STAT_NAME), Mathf.Max(value, 0));

			_statsContainer.UnregisterBaseValueHandler(STAT_NAME);
			_statsContainer.SetStatBaseValue(STAT_NAME, value);
			Assert.AreEqual(_statsContainer.GetStatBaseValue(STAT_NAME), value);
		}
	}
}
