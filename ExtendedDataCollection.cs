using ACadSharp.Tables;
using System;
using System.Collections.Generic;

namespace ACadSharp
{
	public class ExtendedDataDictionary
	{
		private Dictionary<AppId, ExtendedData> _data;

		/// <summary>
		/// Gets the number of ExtendedData entries in the dictionary.
		/// </summary>
		public int Count => _data?.Count ?? 0;

		/// <summary>
		/// Gets a value indicating whether this dictionary has any data.
		/// </summary>
		/// <remarks>
		/// Use this property to check for data without triggering lazy initialization.
		/// </remarks>
		public bool HasData => _data != null && _data.Count > 0;

		/// <summary>Add ExtendedData for a specific AppId to the Dictionary.</summary>
		/// <param name="app">The AppId object.</param>
		/// <param name="edata">The ExtendedData object.</param>
		public void Add(AppId app, ExtendedData edata)
		{
			if (_data == null)
			{
				_data = new Dictionary<AppId, ExtendedData>();
			}
			this._data.Add(app, edata);
		}

		/// <summary>Get ExtendedData for a specific AppId from the Dictionary.</summary>
		/// <param name="app">The AppId object.</param>
		public ExtendedData Get(AppId app)
		{
			if (_data == null)
			{
				throw new KeyNotFoundException($"The given key '{app}' was not present in the dictionary.");
			}
			return this._data[app];
		}

		/// <summary>Try to get ExtendedData for a specific AppId from the Dictionary.</summary>
		/// <param name="app">The AppId object.</param>
		/// <param name="value">ExtendedData object.</param>
		public bool TryGet(AppId app, out ExtendedData value)
		{
			if (_data == null)
			{
				value = null;
				return false;
			}
			return this._data.TryGetValue(app, out value);
		}

		/// <summary>Check whether a AppId is given in the Dictionary.</summary>
		/// <param name="app">The AppId object.</param>
		public bool ContainsKey(AppId app)
		{
			return _data != null && this._data.ContainsKey(app);
		}

		/// <summary>Clear all Dictionary entries.</summary>
		public void Clear()
		{
			this._data?.Clear();
		}
	}

	public class ExtendedData
	{
		private List<ExtendedDataRecord> _data;

		/// <summary>
		/// Gets the data records for this extended data.
		/// </summary>
		/// <remarks>
		/// The list is lazily initialized to reduce memory allocation.
		/// </remarks>
		public List<ExtendedDataRecord> Data
		{
			get
			{
				if (_data == null)
				{
					_data = new List<ExtendedDataRecord>();
				}
				return _data;
			}
			set { _data = value; }
		}

		/// <summary>
		/// Gets a value indicating whether this extended data has any records.
		/// </summary>
		public bool HasData => _data != null && _data.Count > 0;

		/// <summary>
		/// Gets the number of records in this extended data.
		/// </summary>
		public int Count => _data?.Count ?? 0;
	}

	public class ExtendedDataRecord
	{
		public DxfCode Code
		{
			get { return this._code; }
			set
			{
				if (value < DxfCode.ExtendedDataAsciiString)
				{
					throw new ArgumentException($"Dxf code for ExtendedDataRecord is not valid: {value}", nameof(value));
				}

				this._code = value;
			}
		}

		public object Value { get; set; }

		private DxfCode _code;

		public ExtendedDataRecord(DxfCode dxfCode, object value)
		{
			this.Code = dxfCode;
			this.Value = value;
		}
	}
}
