namespace Generator.Equals.Tests.Records
{
    public partial class PrimitiveEquality
    {
        public class EqualsTest : EqualityTestCase
        {
            public override object Factory1() => new Sample("Dave", 35);
            public override bool EqualsOperator(object value1, object value2) => (Sample) value1 == (Sample) value2;
            public override bool NotEqualsOperator(object value1, object value2) => (Sample) value1 != (Sample) value2;
        }
        
        public class NotEqualsTest : EqualityTestCase
        {

            public override bool Expected => false;
            public override object Factory1() => new Sample("Dave", 35);
            public override object Factory2() => new Sample("Joe", 77);
            public override bool EqualsOperator(object value1, object value2) => (Sample) value1 == (Sample) value2;
            public override bool NotEqualsOperator(object value1, object value2) => (Sample) value1 != (Sample) value2;
        }
    }
}