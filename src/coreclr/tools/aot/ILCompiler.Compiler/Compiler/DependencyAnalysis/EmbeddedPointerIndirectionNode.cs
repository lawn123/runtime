// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// An <see cref="EmbeddedObjectNode"/> whose sole value is a pointer to a different <see cref="ISymbolNode"/>.
    /// <typeparamref name="TTarget"/> represents the node type this pointer points to.
    /// </summary>
    public abstract class EmbeddedPointerIndirectionNode<TTarget> : EmbeddedObjectNode, ISortableSymbolNode
        where TTarget : ISortableSymbolNode
    {
        private TTarget _targetNode;

        /// <summary>
        /// Target symbol this node points to.
        /// </summary>
        public TTarget Target => _targetNode;

        protected internal EmbeddedPointerIndirectionNode(TTarget target)
        {
            _targetNode = target;
        }

        public override bool StaticDependenciesAreComputed => true;

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            if (factory.Target.SupportsRelativePointers)
            {
                dataBuilder.RequireInitialAlignment(sizeof(int));
                dataBuilder.EmitReloc(Target, RelocType.IMAGE_REL_BASED_RELPTR32);
            }
            else
            {
                dataBuilder.RequireInitialPointerAlignment();
                dataBuilder.EmitPointerReloc(Target);
            }
        }

        // At minimum, Target needs to be reported as a static dependency by inheritors.
        public abstract override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory);

        int ISymbolNode.Offset => 0;

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("_embedded_ptr_"u8);
            Target.AppendMangledName(nameMangler, sb);
        }

        public override int ClassCode => -2055384490;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_targetNode, ((EmbeddedPointerIndirectionNode<TTarget>)other)._targetNode);
        }
    }
}
