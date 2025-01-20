import json
import logging
import os
import uuid


# import module in dev mode
if '__file__' in globals():
    from PluginBase import PluginBase, AppBinary, ScanChangeResult


class Plugin(PluginBase):
    def __init__(self, config_json, csharp_logger, logging_level=logging.DEBUG):
        super().__init__(config_json, csharp_logger, logging_level)

    def do_full_scan(self, app_binaries_json: str) -> str:
        app_binaries = self._deserialize_app_binaries(app_binaries_json)
        self.logger.debug(f"app_binaries: {repr(app_binaries)}")

        # app_binaries_file_paths, fs_files, files_to_xxx is full path
        app_binaries_file_paths = [os.path.join(self.library_folder, x.files[0].path) for x in app_binaries]
        fs_files = [os.path.join(dp, f) for dp, dn, filenames in os.walk(self.library_folder) for f in filenames]
        files_to_remove = set(app_binaries_file_paths) - set(fs_files)
        files_to_add = set(fs_files) - set(app_binaries_file_paths)
        self.logger.debug(f"files_to_remove: {repr(files_to_remove)}")
        self.logger.debug(f"files_to_add: {repr(files_to_add)}")

        app_binaries_to_recheck = [x for x in app_binaries
                                   if os.path.join(self.library_folder, x.files[0].path) not in files_to_remove]
        self.logger.debug(f"app_binaries_to_recheck: {repr([os.path.join(self.library_folder, x.files[0].path) for x in app_binaries_to_recheck])}")

        app_binaries_to_remove = [x for x in app_binaries
                                  if os.path.join(self.library_folder, x.files[0].path) in files_to_remove]

        app_binaries_to_add = []
        for file in files_to_add:
            try:
                self.logger.info(f"Adding file: {file}")
                file_entry = self._get_file_entry(file, self.library_folder)
                app_binaries_to_add.append(AppBinary(os.path.basename(file), os.path.relpath(file, file),
                                                     file_entry.size_bytes, [file_entry], uuid.uuid4()))
            except Exception as ex:
                self.logger.error(f"Failed to get file entry for {file}: {ex}")
                continue

        app_binaries_to_update = []
        for app_binary in app_binaries_to_recheck:
            full_path = None
            try:
                full_path = os.path.join(self.library_folder, app_binary.files[0].path)
                # NOTE: datetime.utcfromtimestamp does not contain timezone info, use self._getmtime_utc instead
                if self.force_calc_digest or self._getmtime_utc(full_path) != app_binary.files[0].last_write_utc:
                    self.logger.info(f"Updating file: {full_path}")
                    file_entry = self._get_file_entry(full_path, self.library_folder)
                    if file_entry.sha256 != app_binary.files[0].sha256:
                        self.logger.info(f"Updating file {full_path} for its changed SHA256")
                        app_binaries_to_update.append(AppBinary(app_binary.name, app_binary.path,
                                                                file_entry.size_bytes, [file_entry], uuid.uuid4()))
                    else:
                        self.logger.info(f"Updating file {full_path} for its changed LastWriteUtc")
                        app_binary.files[0].last_write_utc = file_entry.last_write_utc
                        app_binaries_to_update.append(app_binary)
                else:
                    self.logger.info(f"Skipping file {full_path} because its last_write_utc is the same")
            except Exception as ex:
                self.logger.error(f"Failed to update file {full_path}: {ex}")
                continue

        return json.dumps(ScanChangeResult(app_binaries_to_remove, app_binaries_to_add,
                                           app_binaries_to_update).to_dict(),
                          ensure_ascii=False)


# test method
if __name__ == '__main__':
    with open('PluginConfig.json', encoding='utf-8') as f:
        config_json = f.read()
    plugin = Plugin(config_json, None, logging.DEBUG)
    result = plugin.do_full_scan('[]')
    print(json.dumps(json.loads(result), ensure_ascii=False, indent=2))
