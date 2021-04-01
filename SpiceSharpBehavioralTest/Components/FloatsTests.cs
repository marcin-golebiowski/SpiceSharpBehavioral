using NUnit.Framework;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;

namespace SpiceSharpBehavioralTest.Components
{
    [TestFixture]
    public class FloatsTests
    {
        [Test]
        public void When_DirectOutputOp_Expect_Reference()
        {
            var ckt = new Circuit(
                new CurrentSource("I1", "0", "N1", 0.025),
                new VoltageSource("X1.Vmas", "N1", "X1.msx", 1),
                new Resistor("X1.Rmas", "X1.msx", "X1.msy", 100),
                new BehavioralVoltageSource("X1.Exm", "X1.msy", "0", "V(N1)"),
                new CurrentControlledVoltageSource("X1.Hmss", "X1.mss", "0", "X1.Vmas", 1),
                new BehavioralCurrentSource("X1.Guv", "0", "N2", "V(X1.mss)"),
                new Resistor("X1.Ruv", "0", "N2", 1));
            var op = new OP("op");

            op.Run(ckt);
        }
    }
}
