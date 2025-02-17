local cjson_safe = require("cjson.safe")
local aes = require("resty.aes")

local _M = {}

-- configs
local config_file_path = "/path/to/config.json"

local cached_config
local cached_aes_enc

-- @param file_path: config file path
-- @return table: config or nil, err
function _M.read_config(file_path)
    if not file_path or file_path == "" then
        return nil, "File path is required"
    end

    -- open file
    local file, err = io.open(file_path, "r")
    if not file then
        return nil, "Failed to open file: " .. err
    end

    -- read file
    local content = file:read("*a")
    file:close()

    -- parse json
    local config, parse_err = cjson_safe.decode(content)
    if not config then
        return nil, "Failed to parse JSON: " .. (parse_err or "unknown error")
    end

    return config, nil
end

-- cached get config
function _M.get_config()
    if not _M.cached_config then
        local config, err = _M.read_config(config_file_path)
        if not config then
            return nil, err
        end
        _M.cached_config = config
    end
    return _M.cached_config
end

-- cached aes_encryptor
function _M.get_aes_enc()
    if not _M.cached_aes_enc then
        local config, err = _M.get_config()
        if not config then
            return nil, err
        end
        if not config.filedl_aes_key then
            return nil, "Missing filedl_aes_key in config"
        end
        _M.cached_aes_enc = aes:new(config.filedl_aes_key)
    end
    return _M.cached_aes_enc
end

return _M