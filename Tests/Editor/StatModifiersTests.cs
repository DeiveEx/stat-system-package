using NUnit.Framework;

namespace DeiveEx.StatSystem.EditorTests
{
    [Category("StatsSystem")]
    public class StatModifiersTests
    {
        //Relative tolerance for float accumulation, similar to what Mathf.Approximately does
        private const float TOLERANCE_PERCENT = 0.001f;

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
				StatModifier modifier = new AdditiveModifier(id + i, magnitudes[i]);
				_statsContainer.ApplyModifier(TestStat.A, modifier);
				total += magnitudes[i];
			}

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(total).Within(TOLERANCE_PERCENT).Percent);

			for (int i = 0; i < magnitudes.Length; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(initialValue).Within(TOLERANCE_PERCENT).Percent);
		}

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
				StatModifier modifier = new MultiplicativeModifier(id + i, magnitudes[i]);
				_statsContainer.ApplyModifier(TestStat.A, modifier);
				total += magnitudes[i];
			}

			var expectedResult = initialValue + (initialValue * total);
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(expectedResult).Within(TOLERANCE_PERCENT).Percent);

			for (int i = 0; i < magnitudes.Length; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(initialValue).Within(TOLERANCE_PERCENT).Percent);
		}

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
				StatModifier modifier = new OverrideModifier(id + i, magnitudesOrderedByPriority[i], i);
				_statsContainer.ApplyModifier(TestStat.A, modifier);
			}

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(magnitudeWithHighestPriority));

			for (int i = 0; i < magnitudesOrderedByPriority.Length; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(initialValue));
		}

		[Test]
		public void Last_Applied_Override_Wins_When_Priorities_Are_Equal()
		{
			_statsContainer.ApplyModifier(TestStat.A, new OverrideModifier("first", 10, 0));
			_statsContainer.ApplyModifier(TestStat.A, new OverrideModifier("second", 20, 0));

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(20));

			//A higher priority override still wins over a later applied one
			_statsContainer.ApplyModifier(TestStat.A, new OverrideModifier("highPriority", 30, 1));
			_statsContainer.ApplyModifier(TestStat.A, new OverrideModifier("third", 40, 0));

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(30));
		}

		[TestCase(10, .1f, 0)]
		[TestCase(0, -.5f, 0)]
		[TestCase(10, .5f, 1)]
		public void Is_Value_Correctly_Affected_By_Different_Modifiers(float additiveMagnitude, float multiplicativeMagnitude, float overrideMagnitude)
		{
			float initialValue = _statsContainer.GetStat(TestStat.A);
			string id = "test";
			float expectedValue = 0;

			StatModifier addModifier = new AdditiveModifier(id + 0, additiveMagnitude);
			StatModifier multModifier = new MultiplicativeModifier(id + 1, multiplicativeMagnitude);
			StatModifier overrideModifier = new OverrideModifier(id + 2, overrideMagnitude);

			_statsContainer.ApplyModifier(TestStat.A, addModifier);
			expectedValue = initialValue + additiveMagnitude;
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(expectedValue).Within(TOLERANCE_PERCENT).Percent);

			_statsContainer.ApplyModifier(TestStat.A, multModifier);
			expectedValue += (expectedValue * multiplicativeMagnitude);
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(expectedValue).Within(TOLERANCE_PERCENT).Percent);

			_statsContainer.ApplyModifier(TestStat.A, overrideModifier);
			expectedValue = overrideMagnitude;
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(expectedValue));

			for (int i = 0; i < 3; i++)
			{
				_statsContainer.RemoveModifier(TestStat.A, id + i);
			}

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(initialValue).Within(TOLERANCE_PERCENT).Percent);
		}

		[Test]
		public void Custom_Modifier_Applied_Correctly()
		{
			StatModifier modifier = new CustomCalculationModifier("test", (baseValue, currentValue) => baseValue / 2f);

			_statsContainer.ApplyModifier(TestStat.A, modifier);
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(_statsContainer.GetStatBaseValue(TestStat.A) / 2f));
		}

		[Test]
		public void Custom_Modifier_Is_Skipped_When_Override_Is_Active()
		{
			_statsContainer.ApplyModifier(TestStat.A, new CustomCalculationModifier("custom", (baseValue, currentValue) => currentValue + 5));
			_statsContainer.ApplyModifier(TestStat.A, new OverrideModifier("override", 50));

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(50));

			//Removing the override should bring the custom calculation back
			_statsContainer.RemoveModifier(TestStat.A, "override");
			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(105));
		}

		[Test]
		public void Custom_Modifier_With_ApplyOnOverride_Is_Applied_Over_The_Override()
		{
			_statsContainer.ApplyModifier(TestStat.A, new CustomCalculationModifier("custom", (baseValue, currentValue) => currentValue + 5, applyOnOverride: true));
			_statsContainer.ApplyModifier(TestStat.A, new OverrideModifier("override", 50));

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(55));
		}

		[Test]
		public void Is_Single_Modifier_Correctly_Removed()
		{
			StatModifier modifier = new AdditiveModifier("test", 1);

			_statsContainer.ApplyModifier(TestStat.A, modifier);
			_statsContainer.ApplyModifier(TestStat.A, modifier);

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(_statsContainer.GetStatBaseValue(TestStat.A) + 2));

			_statsContainer.RemoveModifier(TestStat.A, modifier.ID);

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(_statsContainer.GetStatBaseValue(TestStat.A) + 1));
		}

		[Test]
		public void Is_Multiple_Modifiers_Correctly_Removed()
		{
			StatModifier modifier = new AdditiveModifier("test", 1);

			_statsContainer.ApplyModifier(TestStat.A, modifier);
			_statsContainer.ApplyModifier(TestStat.A, modifier);

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(_statsContainer.GetStatBaseValue(TestStat.A) + 2));

			_statsContainer.RemoveModifier(TestStat.A, modifier.ID, true);

			Assert.That(_statsContainer.GetStat(TestStat.A), Is.EqualTo(_statsContainer.GetStatBaseValue(TestStat.A)));
		}
    }
}