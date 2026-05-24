using ACadSharp.Header;
using CSUtilities.IO;
using CSUtilities.Text;
using System;
using System.IO;
using System.Text;

namespace ACadSharp.IO
{
	/// <summary>
	/// Base class for the DWG and DXF readers.
	/// </summary>
	/// <typeparam name="T">Configuration type for the reader.</typeparam>
	public abstract class CadReaderBase<T> : ICadReader
		where T : CadReaderConfiguration, new()
	{
		/// <inheritdoc/>
		public event NotificationEventHandler OnNotification;

		/// <summary>
		/// Reader configuration.
		/// </summary>
		public T Configuration { get; set; } = new();

		//protected CadDocument _document = new CadDocument();

		protected Encoding _encoding = Encoding.Default;

		internal readonly StreamIO _fileStream;

		protected CadReaderBase(NotificationEventHandler notification)
		{
			this.OnNotification += notification;
		}

		protected CadReaderBase(string filename, NotificationEventHandler notification = null) : this(File.ReadAllBytes(filename), notification)
		{
		}

		protected CadReaderBase(byte[] stream, NotificationEventHandler notification = null) : this(notification)
		{
			this._fileStream = new StreamIO(stream);
		}
		protected CadReaderBase(Stream stream, NotificationEventHandler notification = null) : this(notification)
		{
			// If the stream is already a MemoryStream with an accessible buffer, use it directly.
			// Otherwise, copy to byte[] to avoid per-byte virtual dispatch on FileStream.
			if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> seg) && seg.Offset == 0)
			{
				this._fileStream = new StreamIO(stream);
			}
			else if (stream.CanSeek)
			{
				long pos = stream.Position;
				stream.Position = 0;
				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer, 0, buffer.Length);
				stream.Position = pos;
				this._fileStream = new StreamIO(buffer);
			}
			else
			{
				this._fileStream = new StreamIO(stream);
			}
		}

		/// <inheritdoc/>
		public abstract CadDocument Read();

		/// <inheritdoc/>
		public abstract CadHeader ReadHeader();

		/// <inheritdoc/>
		public virtual void Dispose()
		{
			this._fileStream.Dispose();
		}

		protected Encoding getListedEncoding(int code)
		{
			try
			{
#if !NET48
				Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
#endif
				return Encoding.GetEncoding(code);
			}
			catch (Exception ex)
			{
				this.triggerNotification($"Encoding with codee {code} not found, using Windows-1252 as default", NotificationType.Warning, ex);
			}

			return TextEncoding.Windows1252();
		}

		protected void triggerNotification(string message, NotificationType notificationType, Exception ex = null)
		{
			this.onNotificationEvent(null, new NotificationEventArgs(message, notificationType, ex));
		}

		protected void onNotificationEvent(object sender, NotificationEventArgs e)
		{
			this.OnNotification?.Invoke(this, e);
		}
	}
}
