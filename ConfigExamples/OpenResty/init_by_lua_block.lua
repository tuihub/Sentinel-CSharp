init_by_lua_block {
    require "sentinel_config"
    require "cjson.safe"
    require "resty.jwt"
    
    function join_path(...)
        local parts = {...}
        local path = table.concat(parts, "/")

        -- remove dup /
        path = path:gsub("//+", "/")
        return path
    end
    
    function get_filename_from_path(path)
        local filename = path:match("^.+/(.+)$")
        if not filename then
            return path
        end
        return filename
    end
}