using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.Utils
{
    // https://github.com/facebook-csharp-sdk/combination-stream/blob/master/src/CombinationStream-Net20/CombinationStream.cs
    public class CombinationStream : Stream, IDisposable
    {
        private readonly IEnumerable<Stream> _streams;
        private IEnumerator<Stream> _currentStreamEnumerator;
        private Stream? _currentStream;
        private long _position;

        public CombinationStream(IEnumerable<Stream> streams)
        {
            if (streams == null)
                throw new ArgumentNullException(nameof(streams));

            _streams = streams;
            _currentStreamEnumerator = _streams.GetEnumerator();
            _currentStream = _currentStreamEnumerator.MoveNext() ? _currentStreamEnumerator.Current : null;
            if (_currentStream == null)
                throw new ArgumentException("The streams collection is empty.", nameof(streams));
        }

        public override bool CanRead => _currentStream?.CanRead ?? false;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _streams.Select(s => s.Length).Sum();

        public override long Position
        {
            get => _position;
            set => throw new NotImplementedException();
        }

        public override void Flush()
        {
            _currentStream?.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = 0;
            int buffPostion = offset;

            while (count > 0)
            {
                int bytesRead = _currentStream!.Read(buffer, buffPostion, count);
                result += bytesRead;
                buffPostion += bytesRead;
                _position += bytesRead;

                if (bytesRead <= count)
                    count -= bytesRead;

                if (count > 0)
                {
                    if (_currentStreamEnumerator.MoveNext() == false)
                        break;

                    _currentStream = _currentStreamEnumerator.Current;
                }
            }

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("CombinationStream is not seekable.");
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("CombinationStream is not writeable.");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var stream in _streams)
            {
                stream.Dispose();
            }
        }
    }
}
