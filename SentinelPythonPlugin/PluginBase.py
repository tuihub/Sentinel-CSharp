import base64
import datetime
import hashlib
import json
import logging
import math
import os
import uuid


class FileEntryChunk:
    def __init__(self, offset_bytes: int, size_bytes: int, sha256: bytes):
        self.offset_bytes: int = offset_bytes
        self.size_bytes: int = size_bytes
        self.sha256: bytes = sha256

    def to_dict(self) -> dict:
        return {
            'offset_bytes': self.offset_bytes,
            'size_bytes': self.size_bytes,
            'sha256': base64.b64encode(self.sha256).decode('ascii')
        }


class FileEntry:
    def __init__(self, path: str, size_bytes: int, sha256: bytes, chunks: list[FileEntryChunk],
                 last_write_utc: datetime.datetime):
        self.path: str = path
        self.size_bytes: int = size_bytes
        self.sha256: bytes = sha256
        self.chunks: list[FileEntryChunk] = chunks
        self.last_write_utc: datetime.datetime = last_write_utc

    def to_dict(self) -> dict:
        return {
            'path': self.path,
            'size_bytes': self.size_bytes,
            'sha256': base64.b64encode(self.sha256).decode('ascii'),
            'chunks': [c.to_dict() for c in self.chunks],
            'last_write_utc': self.last_write_utc.isoformat().replace('+00:00', 'Z')
        }


class AppBinary:
    def __init__(self, name: str, path: str, size_bytes: int, files: list[FileEntry], guid: uuid.UUID):
        self.name: str = name
        self.path: str = path
        self.size_bytes: int = size_bytes
        self.files: list[FileEntry] = files
        self.guid: uuid.UUID = guid

    def to_dict(self) -> dict:
        return {
            'name': self.name,
            'path': self.path,
            'size_bytes': self.size_bytes,
            'files': [f.to_dict() for f in self.files],
            'guid': str(self.guid)
        }


class ScanChangeResult:
    def __init__(self, app_binaries_to_remove: list[AppBinary], app_binaries_to_add: list[AppBinary],
                 app_binaries_to_update: list[AppBinary]):
        self.app_binaries_to_remove: list[AppBinary] = app_binaries_to_remove
        self.app_binaries_to_add: list[AppBinary] = app_binaries_to_add
        self.app_binaries_to_update: list[AppBinary] = app_binaries_to_update

    def to_dict(self) -> dict:
        return {
            'app_binaries_to_remove': [b.to_dict() for b in self.app_binaries_to_remove],
            'app_binaries_to_add': [b.to_dict() for b in self.app_binaries_to_add],
            'app_binaries_to_update': [b.to_dict() for b in self.app_binaries_to_update]
        }


class CSharpLoggingHandler(logging.Handler):
    def __init__(self, csharp_logger):
        super().__init__()
        self.csharp_logger = csharp_logger

    def emit(self, record):
        # Format the log message
        msg = self.format(record)

        # Map Python log levels to C# ILogger methods
        if record.levelno >= logging.CRITICAL:
            self.csharp_logger.LogCritical(msg)
        elif record.levelno >= logging.ERROR:
            self.csharp_logger.LogError(msg)
        elif record.levelno >= logging.WARNING:
            self.csharp_logger.LogWarning(msg)
        elif record.levelno >= logging.INFO:
            self.csharp_logger.LogInformation(msg)
        elif record.levelno >= logging.DEBUG:
            self.csharp_logger.LogDebug(msg)
        else:
            self.csharp_logger.LogTrace(msg)


class PluginBase:
    def __init__(self, config_json, csharp_logger, logging_level):
        if csharp_logger:
            logger = logging.getLogger()
            logger.setLevel(logging_level)
            logger.addHandler(CSharpLoggingHandler(csharp_logger))
        else:
            logging.basicConfig(
                level=logging.DEBUG,
                format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
                datefmt='%Y-%m-%d %H:%M:%S'
            )
            logger = logging.getLogger()
        self.logger = logger
        self.logger.debug(f"PluginBase: Initializing plugin with config: {config_json}")
        config = json.loads(config_json)
        self.library_name: str = config['LibraryName']
        self.library_folder: str = config['LibraryFolder']
        self.chunk_size_bytes: int = int(config['ChunkSizeBytes'])
        self.force_calc_digest: bool = bool(config['ForceCalcDigest'])
        self.custom_config: dict = config['PythonScriptCustomConfig']

    def _get_file_entry(self, file_full_path: str, base_path: str, calc_sha256: bool = True,
                        buffer_size_bytes: int = 8192) -> FileEntry:
        self.logger.info(f"_get_file_entry: Getting file entry for {file_full_path}")
        file_size = os.path.getsize(file_full_path)
        if self.chunk_size_bytes % buffer_size_bytes != 0:
            raise ValueError("Chunk size must be a multiple of buffer size.")
        chunk_count = math.ceil(file_size / self.chunk_size_bytes)
        chunks = []
        last_write_utc = datetime.datetime.utcfromtimestamp(os.path.getmtime(file_full_path))

        if calc_sha256:
            sha256_file = hashlib.sha256()
            self.logger.debug(f"_get_file_entry: Calculating SHA256 for {file_full_path}")
            with open(file_full_path, 'rb') as file_stream:
                for i in range(chunk_count):
                    self.logger.debug(f"_get_file_entry: Processing chunk {i + 1}/{chunk_count}")
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
            self.logger.debug(f"_get_file_entry: Skipping SHA256 calculation for {file_full_path}")
            for i in range(chunk_count):
                offset_bytes = i * self.chunk_size_bytes
                end_bytes = min(offset_bytes + self.chunk_size_bytes, file_size)
                current_chunk_size = end_bytes - offset_bytes
                chunks.append(FileEntryChunk(offset_bytes, current_chunk_size, b'\x00' * 32))
            file_hash = b'\x00' * 32

        relative_path = os.path.relpath(file_full_path, base_path)
        return FileEntry(relative_path, file_size, file_hash, chunks, last_write_utc)

    def _deserialize_app_binaries(self, app_binaries_json: str) -> list[AppBinary]:
        return [AppBinary(x['name'],
                          x['path'],
                          x['size_bytes'],
                          [FileEntry(f['path'],
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
