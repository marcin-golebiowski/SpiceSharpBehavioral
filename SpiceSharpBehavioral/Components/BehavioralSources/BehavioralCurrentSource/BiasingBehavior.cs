﻿using SpiceSharp.Behaviors;
using SpiceSharp.ParameterSets;
using System;
using SpiceSharp.Components.BehavioralComponents;
using SpiceSharp.Algebra;
using SpiceSharp.Simulations;
using SpiceSharp.Components.CommonBehaviors;
using System.Collections.Generic;
using SpiceSharpBehavioral.Parsers.Nodes;
using SpiceSharp.Attributes;

namespace SpiceSharp.Components.BehavioralCurrentSourceBehaviors
{
    /// <summary>
    /// Biasing behavior for a <see cref="BehavioralCurrentSource"/>.
    /// </summary>
    /// <seealso cref="Behavior" />
    /// <seealso cref="IBiasingBehavior" />
    [BehaviorFor(typeof(BehavioralCurrentSource), typeof(IBiasingBehavior))]
    public class BiasingBehavior : Behavior,
        IBiasingBehavior
    {
        private readonly OnePort<double> _variables;
        private readonly ElementSet<double> _elements;
        private readonly Func<double> _value;
        
        /// <summary>
        /// The functions that compute the derivatives.
        /// </summary>
        protected readonly Tuple<VariableNode, IVariable<double>, Func<double>>[] Functions;

        /// <summary>
        /// Gets the current.
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        [ParameterName("i"), ParameterName("c"), ParameterInfo("The instantaneous current")]
        public double Current { get; private set; }

        /// <summary>
        /// Gets the voltage.
        /// </summary>
        /// <value>
        /// The voltage.
        /// </value>
        [ParameterName("v"), ParameterInfo("The instantaneous voltage")]
        public double Voltage => _variables.Positive.Value - _variables.Negative.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="BiasingBehavior"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <c>null</c>.</exception>
        public BiasingBehavior(BehavioralBindingContext context)
            : base(context)
        {
            var bp = context.GetParameterSet<Parameters>();
            var state = context.GetState<IBiasingSimulationState>();
            _variables = new OnePort<double>(
                state.GetSharedVariable(context.Nodes[0]),
                state.GetSharedVariable(context.Nodes[1]));

            // Build the functions using our variable
            var df = context.Derivatives;

            // TODO: Take this from parameters
            var variables = new Dictionary<VariableNode, IVariable<double>>();
            foreach (var pair in df)
            {
                switch (pair.Key.NodeType)
                {
                    case NodeTypes.Voltage: variables.Add(pair.Key, state.GetSharedVariable(pair.Key.Name)); break;
                    case NodeTypes.Current: variables.Add(pair.Key, context.Branches[pair.Key].GetValue<IBranchedBehavior<double>>().Branch); break;
                    default:
                        throw new Exception("Invalid variable");
                }
            }
            var builder = bp.BuilderFactory(variables);

            // Let's build the derivative functions and get their matrix locations/rhs locations
            _value = builder.Build(bp.Function);
            Functions = new Tuple<VariableNode, IVariable<double>, Func<double>>[df.Count];
            var matLocs = new MatrixLocation[df.Count * 2];
            var rhsLocs = _variables.GetRhsIndices(state.Map);
            int index = 0;
            foreach (var pair in df)
            {
                var variable = variables[pair.Key];
                var func = builder.Build(pair.Value);
                Functions[index] = Tuple.Create(pair.Key, variable, func);
                matLocs[index * 2] = new MatrixLocation(rhsLocs[0], state.Map[variable]);
                matLocs[index * 2 + 1] = new MatrixLocation(rhsLocs[1], state.Map[variable]);
                index++;
            }

            // Get the matrix elements
            _elements = new ElementSet<double>(state.Solver, matLocs, rhsLocs);
        }

        /// <inheritdoc/>
        void IBiasingBehavior.Load()
        {
            double[] values = new double[Functions.Length * 2 + 2];
            var total = Current = _value();

            int i;
            for (i = 0; i < Functions.Length; i++)
            {
                var df = Functions[i].Item3.Invoke();
                total -= Functions[i].Item2.Value * df;
                values[i * 2] = df;
                values[i * 2 + 1] = -df;
            }
            values[i * 2] = -total;
            values[i * 2 + 1] = total;
            _elements.Add(values);
        }
    }
}