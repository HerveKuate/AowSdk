﻿using System;
using System.IO;

namespace Aow2.Serialization.Internal.Builders
{
	class BoolFormatterBuilder: IFormatterBuilder
	{
		private class BoolFormatter: Formatter<bool>
		{
			public override void Serialize( Stream outStream, bool value )
			{
			}

			public override bool Deserialize( Stream inStream, long offset, long length ) => true;

			public override bool ShouldSkipField( bool value ) => !value;
		}

		public bool CanCreate( FormatterRequest request ) => request.Type == typeof( bool );

		public IFormatter Create( Type type ) => new BoolFormatter();
	}
}
