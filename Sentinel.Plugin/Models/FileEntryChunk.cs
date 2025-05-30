﻿namespace Sentinel.Plugin.Models
{
    public record FileEntryChunk
    {
        public long OffsetBytes { get; init; }
        public long SizeBytes { get; init; }
        public byte[] Sha256 { get; init; }

        public FileEntryChunk(long offsetBytes, long sizeBytes, byte[] sha256)
        {
            OffsetBytes = offsetBytes;
            SizeBytes = sizeBytes;
            Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
            if (sha256.Length != 32) { throw new ArgumentException("SHA256 must be 32 bytes long."); }
        }

        public virtual bool Equals(FileEntryChunk? other)
        {
            if (other is null) { return false; }
            return OffsetBytes.Equals(other.OffsetBytes) &&
                   SizeBytes.Equals(other.SizeBytes) &&
                   Sha256.SequenceEqual(other.Sha256);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(OffsetBytes);
            hash.Add(SizeBytes);
            foreach (var b in Sha256)
            {
                hash.Add(b);
            }
            return hash.ToHashCode();
        }

        public string ToFullHumanString(int indent = 2)
        {
            string indentStr = new string('\t', indent);
            return $"{indentStr}{nameof(FileEntryChunk)} {{ OffsetBytes: {OffsetBytes}, SizeBytes: {SizeBytes}, " +
                $"Sha256: {BitConverter.ToString(Sha256).Replace("-", "")} }}";
        }
    }
}
