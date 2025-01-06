import base64
import datetime
import hashlib
import json
import math
import os
import uuid


class PluginBase:
    def __init__(self, config):
        self.library_name = config.LibraryName
        self.library_folder = config.LibraryFolder
        self.chunk_size_bytes = config.ChunkSizeBytes
        self.force_calc_digest = config.ForceCalcDigest
        self.custom_config = config.ScriptConfig

    def _get_file_entry(self, file_full_path, base_path, calc_sha256=True, buffer_size_bytes=8192):
        file_size = os.path.getsize(file_full_path)
        if self.chunk_size_bytes % buffer_size_bytes != 0:
            raise ValueError("Chunk size must be a multiple of buffer size.")
        chunk_count = math.ceil(file_size / self.chunk_size_bytes)
        chunks = []
        last_write_utc = datetime.datetime.utcfromtimestamp(os.path.getmtime(file_full_path))

        if calc_sha256:
            sha256_file = hashlib.sha256()
            with open(file_full_path, 'rb') as file_stream:
                for i in range(chunk_count):
                    offset_bytes = i * self.chunk_size_bytes
                    current_chunk_size_bytes = min(offset_bytes + self.chunk_size_bytes, file_size) - offset_bytes

                    sha256_chunk = hashlib.sha256()
                    bytes_read = 0
                    while bytes_read < current_chunk_size_bytes:
                        read_size = min(buffer_size_bytes, current_chunk_size_bytes - bytes_read)
                        buffer = file_stream.read(read_size)

                        if not buffer:
                            break

                        sha256_chunk.update(buffer)
                        sha256_file.update(buffer)
                        bytes_read += len(buffer)

                    chunk_hash = sha256_chunk.digest()
                    chunks.append(FileEntryChunk(offset_bytes, current_chunk_size_bytes, chunk_hash))

            file_hash = sha256_file.digest()
        else:
            for i in range(chunk_count):
                offset_bytes = i * self.chunk_size_bytes
                end_bytes = min(offset_bytes + self.chunk_size_bytes, file_size)
                current_chunk_size = end_bytes - offset_bytes
                chunks.append(FileEntryChunk(offset_bytes, current_chunk_size, b'\x00' * 32))
            file_hash = b'\x00' * 32

        relative_path = os.path.relpath(file_full_path, base_path)
        return FileEntry(relative_path, file_size, file_hash, chunks, last_write_utc)

    def _deserialize_app_binaries(self, app_binaries_json):
        return [AppBinary(x['path'],
                          x['size_bytes'],
                          [FileEntry(f['file_path'],
                                     f['size_bytes'],
                                     base64.b64decode(f['sha256']),
                                     [FileEntryChunk(c['offset_bytes'],
                                                     c['size_bytes'],
                                                     base64.b64decode(c['sha256']))
                                      for c in f['chunks']],
                                     datetime.datetime.fromisoformat(f['last_write_utc'].replace('Z', '+00:00')))
                           for f in x['files']],
                          uuid.UUID(x['guid']))
                for x in json.loads(app_binaries_json)]


class AppBinary:
    def __init__(self, path, size_bytes, files, guid):
        self.path = path
        self.size_bytes = size_bytes
        self.files = files
        self.guid = guid

    def to_dict(self):
        return {
            'path': self.path,
            'size_bytes': self.size_bytes,
            'files': [f.to_dict() for f in self.files],
            'guid': str(self.guid)
        }


class FileEntry:
    def __init__(self, file_path, size_bytes, sha256, chunks, last_write_utc):
        self.file_path = file_path
        self.size_bytes = size_bytes
        self.sha256 = sha256
        self.chunks = chunks
        self.last_write_utc = last_write_utc

    def to_dict(self):
        return {
            'file_path': self.file_path,
            'size_bytes': self.size_bytes,
            'sha256': base64.b64encode(self.sha256).decode('ascii'),
            'chunks': [c.to_dict() for c in self.chunks],
            'last_write_utc': self.last_write_utc.isoformat().replace('+00:00', 'Z')
        }


class FileEntryChunk:
    def __init__(self, offset_bytes, size_bytes, sha256):
        self.offset_bytes = offset_bytes
        self.size_bytes = size_bytes
        self.sha256 = sha256

    def to_dict(self):
        return {
            'offset_bytes': self.offset_bytes,
            'size_bytes': self.size_bytes,
            'sha256': base64.b64encode(self.sha256).decode('ascii')
        }


class ScanChangeResult:
    def __init__(self, app_binaries_to_remove, app_binaries_to_add, app_binaries_to_update):
        self.app_binaries_to_remove = app_binaries_to_remove
        self.app_binaries_to_add = app_binaries_to_add
        self.app_binaries_to_update = app_binaries_to_update

    def to_dict(self):
        return {
            'app_binaries_to_remove': [b.to_dict() for b in self.app_binaries_to_remove],
            'app_binaries_to_add': [b.to_dict() for b in self.app_binaries_to_add],
            'app_binaries_to_update': [b.to_dict() for b in self.app_binaries_to_update]
        }
