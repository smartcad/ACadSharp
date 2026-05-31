namespace ACadSharp
{
	public static class GroupCodeValue
	{
		public static GroupCodeValueType TransformValue(int code)
		{
            return code switch
            {
                >= 0 and <= 4 => GroupCodeValueType.String,
                5 => GroupCodeValueType.Handle,
                >= 6 and <= 9 => GroupCodeValueType.String,
                >= 10 and <= 39 => GroupCodeValueType.Point3D,
                >= 40 and <= 59 => GroupCodeValueType.Double,
                >= 60 and <= 79 => GroupCodeValueType.Int16,
                >= 90 and <= 99 => GroupCodeValueType.Int32,
                100 => GroupCodeValueType.String,
                101 => GroupCodeValueType.String,
                102 => GroupCodeValueType.String,
                105 => GroupCodeValueType.Handle,
                >= 110 and <= 119 => GroupCodeValueType.Double,
                >= 120 and <= 129 => GroupCodeValueType.Double,
                >= 130 and <= 139 => GroupCodeValueType.Double,
                >= 140 and <= 149 => GroupCodeValueType.Double,
                >= 160 and <= 169 => GroupCodeValueType.Int64,
                >= 170 and <= 179 => GroupCodeValueType.Int16,
                >= 210 and <= 239 => GroupCodeValueType.Double,
                >= 270 and <= 279 => GroupCodeValueType.Int16,
                >= 280 and <= 289 => GroupCodeValueType.Byte,
                >= 290 and <= 299 => GroupCodeValueType.Bool,
                >= 300 and <= 309 => GroupCodeValueType.String,
                >= 310 and <= 319 => GroupCodeValueType.Chunk,
                >= 320 and <= 329 => GroupCodeValueType.Handle,
                >= 330 and <= 369 => GroupCodeValueType.ObjectId,
                >= 370 and <= 379 => GroupCodeValueType.Int16,
                >= 380 and <= 389 => GroupCodeValueType.Int16,
                >= 390 and <= 399 => GroupCodeValueType.ObjectId,
                >= 400 and <= 409 => GroupCodeValueType.Int16,
                >= 410 and <= 419 => GroupCodeValueType.String,
                >= 420 and <= 429 => GroupCodeValueType.Int32,
                >= 430 and <= 439 => GroupCodeValueType.String,
                >= 440 and <= 449 => GroupCodeValueType.Int32,
                >= 450 and <= 459 => GroupCodeValueType.Int32,
                >= 460 and <= 469 => GroupCodeValueType.Double,
                >= 470 and <= 479 => GroupCodeValueType.String,
                >= 480 and <= 481 => GroupCodeValueType.Handle,
                999 => GroupCodeValueType.Comment,
                >= 1000 and <= 1003 => GroupCodeValueType.ExtendedDataString,
                1004 => GroupCodeValueType.ExtendedDataChunk,
                >= 1005 and <= 1009 => GroupCodeValueType.ExtendedDataHandle,
                >= 1010 and <= 1059 => GroupCodeValueType.ExtendedDataDouble,
                >= 1060 and <= 1070 => GroupCodeValueType.ExtendedDataInt16,
                1071 => GroupCodeValueType.ExtendedDataInt32,
                _ => GroupCodeValueType.None
            };
        }
	}
}