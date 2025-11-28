using ACadSharp.Entities;
using System;
using System.Collections;

namespace ACadSharp
{
	public interface ISeqendCollection : IEnumerable
	{
		public event EventHandler<Seqend> OnSeqendAdded;

		public event EventHandler<Seqend> OnSeqendRemoved;

		Seqend Seqend { get; }
	}
}
