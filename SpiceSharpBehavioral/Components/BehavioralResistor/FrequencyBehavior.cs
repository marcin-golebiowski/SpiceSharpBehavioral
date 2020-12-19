﻿using SpiceSharp.Algebra;
using SpiceSharp.Behaviors;
using SpiceSharp.Simulations;
using System;
using System.Numerics;
using SpiceSharp.Components.BehavioralComponents;
using SpiceSharp.ParameterSets;
using SpiceSharp.Components.CommonBehaviors;
using SpiceSharp.Attributes;
using SpiceSharpBehavioral;
using System.Collections.Generic;
using SpiceSharpBehavioral.Parsers.Nodes;

namespace SpiceSharp.Components.BehavioralResistorBehaviors
{
    /// <summary>
    /// Frequency behavior for a <see cref="BehavioralVoltageSource"/>.
    /// </summary>
    /// <seealso cref="BiasingBehavior" />
    /// <seealso cref="IFrequencyBehavior" />
    /// <seealso cref="IBranchedBehavior{T}"/>
    [BehaviorFor(typeof(BehavioralResistor), typeof(IFrequencyBehavior), 1)]
    public class FrequencyBehavior : BiasingBehavior,
        IFrequencyBehavior,
        IBranchedBehavior<Complex>
    {
        private readonly OnePort<Complex> _variables;
        private readonly IVariable<Complex> _branch;
        private readonly ElementSet<Complex> _elements, _coreElements;
        private readonly Func<Complex>[] _derivatives;

        /// <summary>
        /// Gets the branch equation variable.
        /// </summary>
        /// <value>
        /// The branch equation variable.
        /// </value>
        IVariable<Complex> IBranchedBehavior<Complex>.Branch => _branch;

        /// <summary>
        /// Gets the complex voltage.
        /// </summary>
        /// <value>
        /// The complex voltage.
        /// </value>
        [ParameterName("v"), ParameterName("v_c"), ParameterInfo("The complex voltage")]
        public Complex ComplexVoltage { get; private set; }

        /// <summary>
        /// Gets the complex current.
        /// </summary>
        /// <value>
        /// The complex current.
        /// </value>
        [ParameterName("i"), ParameterName("i_c"), ParameterInfo("The complex current")]
        public Complex ComplexCurrent => _branch.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrequencyBehavior"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <c>null</c>.</exception>
        public FrequencyBehavior(BehavioralBindingContext context)
            : base(context)
        {
            var bp = context.GetParameterSet<Parameters>();
            var state = context.GetState<IComplexSimulationState>();
            _variables = new OnePort<Complex>(
                state.GetSharedVariable(context.Nodes[0]),
                state.GetSharedVariable(context.Nodes[1]));
            _branch = state.CreatePrivateVariable(Name.Combine("branch"), Units.Ampere);

            // Build the functions
            var nVariables = new Dictionary<VariableNode, IVariable<Complex>>(Derivatives.Comparer);
            foreach (var variable in Derivatives.Keys)
            {
                var orig = DerivativeVariables[variable];
                nVariables.Add(variable, new FuncVariable<Complex>(orig.Name, () => orig.Value, orig.Unit));
            }
            var builder = context.CreateBuilder(bp.ComplexBuilderFactory, nVariables);
            _derivatives = new Func<Complex>[Derivatives.Count];
            var rhsLocs = state.Map[_branch];
            var matLocs = new MatrixLocation[Derivatives.Count];
            int index = 0;
            foreach (var pair in Derivatives)
            {
                _derivatives[index] = builder.Build(pair.Value);
                var variable = context.MapNode(state, pair.Key, _branch);
                matLocs[index] = new MatrixLocation(rhsLocs, state.Map[variable]);
                index++;
            }

            // Get the matrix elements
            _elements = new ElementSet<Complex>(state.Solver, matLocs);
            int br = state.Map[_branch];
            int pos = state.Map[_variables.Positive];
            int neg = state.Map[_variables.Negative];
            _coreElements = new ElementSet<Complex>(state.Solver, new[] {
                new MatrixLocation(br, pos),
                new MatrixLocation(br, neg),
                new MatrixLocation(pos, br),
                new MatrixLocation(neg, br)
            });
        }

        /// <summary>
        /// Initializes the frequency behavior.
        /// </summary>
        void IFrequencyBehavior.InitializeParameters()
        {
        }

        /// <summary>
        /// Loads the Y-matrix and right hand side vector.
        /// </summary>
        void IFrequencyBehavior.Load()
        {
            var values = new Complex[_derivatives.Length];
            for (var i = 0; i < _derivatives.Length; i++)
                values[i] = -_derivatives[i]();
            _elements.Add(values);
            _coreElements.Add(1.0, -1.0, 1.0, -1.0);
        }
    }
}