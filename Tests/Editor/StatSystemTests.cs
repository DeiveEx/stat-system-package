using System.Collections.Generic;
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
			_statsContainer = new StatsContainer<TestStat>(new StatCurrentValueResolver<TestStat>(), "testID");
			_statsContainer.AddStat(TestStat.A, 100);
		}

		//TearDown methods are executed after each test
		[TearDown]
		public void TearDown() { }

		[Test]
		public void TestStat_was_created_correctly()
		{
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), 100);
		}

		[Test]
		public void StatsContainer_Created_Correctly()
		{
			Assert.IsNotNull(_statsContainer);
			
			Assert.IsTrue(_statsContainer.StatExists(TestStat.A));
			Assert.AreEqual(_statsContainer.GetStatBaseValue(TestStat.A), 100);
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), 100);
			
			Assert.IsFalse(_statsContainer.StatExists(TestStat.B));
		}

		[Test]
		[TestCase(0)]
		[TestCase(5)]
		[TestCase(-10)]
		public void Stat_Value_is_correctly_changed(float value)
		{
			_statsContainer.SetStat(TestStat.A, value);
			Assert.AreEqual(_statsContainer.GetStatBaseValue(TestStat.A), value);
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), value);
		}

		[Test]
		public void Is_BaseValue_and_Value_Equal_Without_Modifiers()
		{
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), _statsContainer.GetStatBaseValue(TestStat.A));
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
			float initialValue = _statsContainer.GetStat(TestStat.A);
			string id = "testModifier";
			float total = initialValue;

			for (int i = 0; i < magnitudes.Length; i++)
			{
				StatModifier modifier = new StatModifier(id + i, OperationType.Additive, magnitudes[i]);
				_statsContainer.ApplyModifier(TestStat.A, modifier);
				total += magnitudes[i];
			}

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), total);

			for (int i = 0; i < magnitudes.Length; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), initialValue);
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
			float initialValue = _statsContainer.GetStat(TestStat.A);
			string id = "testModifier";
			float total = 0;

			for (int i = 0; i < magnitudes.Length; i++)
			{
				StatModifier modifier = new StatModifier(id + i, OperationType.Multiplicative, magnitudes[i]);
				_statsContainer.ApplyModifier(TestStat.A, modifier);
				total += magnitudes[i];
			}
			
			var expectedResult = initialValue + (initialValue * total);
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), expectedResult);

			for (int i = 0; i < magnitudes.Length; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), initialValue);
		}

		[Test]
		[TestCase(1)]
		[TestCase(1000)]
		[TestCase(1000, 1)]
		[TestCase(1000, 50, 1)]
		public void Is_Value_Correctly_Affected_By_Override_Modifiers(params float[] magnitudesOrderedByPriority)
		{
			float initialValue = _statsContainer.GetStat(TestStat.A);
			string id = "testModifier";
			float magnitudeWithHighestPriority = magnitudesOrderedByPriority[^1];

			for (int i = 0; i < magnitudesOrderedByPriority.Length; i++)
			{
				StatModifier modifier = new StatModifier(id + i, OperationType.Override, magnitudesOrderedByPriority[i], i);
				_statsContainer.ApplyModifier(TestStat.A, modifier);
			}

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), magnitudeWithHighestPriority);

			for (int i = 0; i < magnitudesOrderedByPriority.Length; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), initialValue);
		}

		[Test]
		[TestCase(10, .1f, 0)]
		[TestCase(0, -.5f, 0)]
		[TestCase(10, .5f, 1)]
		public void Is_Value_Correctly_Affected_By_Different_Modifiers(float additiveMagnitude, float multiplicativeMagnitude, float overrideMagnitude)
		{
			float initialValue = _statsContainer.GetStat(TestStat.A);
			string id = "test";
			float expectedValue = 0;

			StatModifier addModifier = new StatModifier(id + 0, OperationType.Additive, additiveMagnitude);
			StatModifier multModifier = new StatModifier(id + 1, OperationType.Multiplicative, multiplicativeMagnitude);
			StatModifier overrideModifier = new StatModifier(id + 2, OperationType.Override, overrideMagnitude);

			_statsContainer.ApplyModifier(TestStat.A, addModifier);
			expectedValue = initialValue + additiveMagnitude;
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), expectedValue);

			_statsContainer.ApplyModifier(TestStat.A, multModifier);
			expectedValue += (expectedValue * multiplicativeMagnitude);
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), expectedValue);

			_statsContainer.ApplyModifier(TestStat.A, overrideModifier);
			expectedValue = overrideMagnitude;
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), expectedValue);

			for (int i = 0; i < 3; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), initialValue);
		}

		[Test]
		public void Custom_Modifier_Applied_Correctly()
		{
			StatModifier modifier = new StatModifier("test", OperationType.Custom, 0, 0, (baseValue, currentValue) => { return baseValue / 2f; });

			_statsContainer.ApplyModifier(TestStat.A, modifier);
			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), _statsContainer.GetStatBaseValue(TestStat.A) / 2f);
		}

		[Test]
		public void Is_Single_Modifier_Correctly_Removed()
		{
			StatModifier modifier = new StatModifier("test", OperationType.Additive, 1);

			_statsContainer.ApplyModifier(TestStat.A, modifier);
			_statsContainer.ApplyModifier(TestStat.A, modifier);

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), _statsContainer.GetStatBaseValue(TestStat.A) + 2);

			_statsContainer.RemoveModifier(TestStat.A, modifier.ID);

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), _statsContainer.GetStatBaseValue(TestStat.A) + 1);
		}

		[Test]
		public void Is_Multiple_Modifiers_Correctly_Removed()
		{
			StatModifier modifier = new StatModifier("test", OperationType.Additive, 1);

			_statsContainer.ApplyModifier(TestStat.A, modifier);
			_statsContainer.ApplyModifier(TestStat.A, modifier);

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), _statsContainer.GetStatBaseValue(TestStat.A) + 2);

			_statsContainer.RemoveModifier(TestStat.A, modifier.ID, true);

			Assert.AreEqual(_statsContainer.GetStat(TestStat.A), _statsContainer.GetStatBaseValue(TestStat.A));
		}

		[Test]
		public void Events_are_Fired_Correctly()
		{
			int statAdded = 0;
			int baseChanged = 0;
			int modifierChanged = 0;

			_statsContainer.StatAdded += (sender, e) => { statAdded++; };
			_statsContainer.StatValueChanged += (sender, e) => { baseChanged++; };
			_statsContainer.ModifierAdded += (sender, e) => { modifierChanged++; };
			_statsContainer.ModifierRemoved += (sender, e) => { modifierChanged--; };

			_statsContainer.AddStat(TestStat.B);
			Assert.AreEqual(statAdded, 1);
			
			_statsContainer.SetStat(TestStat.A, 1);
			Assert.AreEqual(baseChanged, 1);
			Assert.AreEqual(modifierChanged, 0);

			StatModifier modifier = new StatModifier("test", OperationType.Additive, 10);
			_statsContainer.ApplyModifier(TestStat.A, modifier);

			Assert.AreEqual(modifierChanged, 1);

			_statsContainer.RemoveModifier(TestStat.A, modifier.ID);
			Assert.AreEqual(modifierChanged, 0);
		}

		[Test]
		[TestCase(10)]
		[TestCase(1000)]
		[TestCase(0)]
		[TestCase(-10)]
		public void Stat_BaseValue_Handlers_Are_Correctly_Registered_And_Executed(float value)
		{
			_statsContainer.RegisterBaseValueHandler(TestStat.A, (stat, targetValue, container) => Mathf.Max(targetValue, 0));
			_statsContainer.SetStat(TestStat.A, value);
			Assert.AreEqual(_statsContainer.GetStatBaseValue(TestStat.A), Mathf.Max(value, 0));

			_statsContainer.UnregisterBaseValueHandler(TestStat.A);
			_statsContainer.SetStat(TestStat.A, value);
			Assert.AreEqual(_statsContainer.GetStatBaseValue(TestStat.A), value);
		}
	}
}
