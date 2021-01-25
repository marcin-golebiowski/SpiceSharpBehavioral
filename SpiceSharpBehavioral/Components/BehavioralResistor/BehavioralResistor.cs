﻿using SpiceSharp.Components.BehavioralComponents;
using SpiceSharp.Attributes;

namespace SpiceSharp.Components
{
    /// <summary>
    /// A behavioral resistor.
    /// </summary>
    /// <seealso cref="BehavioralComponent"/>
    [Pin(0, "P"), Pin(1, "N")]
    public class BehavioralResistor : BehavioralComponent
    {
        /// <summary>
        /// The behavioral resistor base pin count.
        /// </summary>
        public const int BehavioralResistorPinCount = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="BehavioralResistor"/> class.
        /// </summary>
        /// <param name="name">The name of the entity</param>
        public BehavioralResistor(string name)
            : base(name, BehavioralResistorPinCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BehavioralResistor"/> class.
        /// </summary>
        /// <param name="name">The name of the resistor.</param>
        /// <param name="pos">The positive node.</param>
        /// <param name="neg">The negative node.</param>
        /// <param name="expression">The expression that describes the resistance.</param>
        public BehavioralResistor(string name, string pos, string neg, string expression)
            : this(name)
        {
            Connect(pos, neg);
            Parameters.Expression = expression;
        }
    }
}
