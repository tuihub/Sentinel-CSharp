# Sentinel-CSharp

Sentinel-CSharp��Sentinel��C#ʵ�֣��ṩ��Sentinel�ĺ��Ĺ��ܣ��������ļ����ļ���AppBinaryɨ�衢�ϱ�������OpenResty���õȹ��ܡ�

## ����

- ����֧�֣��������ֲ������
  - SingleFile���������ļ�
  - SubFolder�����ݲ��Դ����ļ������ļ�
  - PythonPluginLoader�����ز�����Python���
    - �ṩPython��SingleFile���ʾ��
- ��̨����֧����Ϊ�ػ��������У�����ɨ���ļ����
- �����ϱ�����Librarian���񼯳ɣ�֧���Զ��ϱ��ļ���Ϣ
- ����չ�ԣ�ͨ�����ϵͳ֧���Զ����ļ������߼�
- OpenResty���ɣ��ṩOpenResty����ʾ����֧���ļ�������֤�ͷ��ʿ���

## ʹ��

### �����в���

Sentinel-CSharp֧�ֶ�������ģʽ��

- �ػ�����ģʽ����ȡ�����ļ�����������

```
# �ػ�����ģʽ����ȡ�����ļ����������У�
dotnet Sentinel.dll daemon [options]
# ���д
dotnet Sentinel.dll d [options]

# ѡ�
# -n, --no-report     ֻ��¼������̨������������ϱ�
```

- �����ģʽ��������

```
# ���ļ����
dotnet Sentinel.dll singlefile [options]
# ���д
dotnet Sentinel.dll sf [options]

# ���ļ��в��
dotnet Sentinel.dll subfolder [options]
# ���д
dotnet Sentinel.dll subf [options]

# Python���
dotnet Sentinel.dll pythonpluginloader [options]
# ���д
dotnet Sentinel.dll ppl [options]
# ѡ��
# -s, --script-path <PATH>  Python�ű�·��
# -c, --script-class <NAME> Python����

# ͨ��ѡ�
# -d, --dir <DIR>           ָ��Ҫɨ���Ŀ¼
# -n, --dry-run             ������ģʽ��������SHA256У���
# --chunk-size <SIZE>       �ֿ��С���ֽڣ���Ĭ��Ϊ67108864��64 MiB��
# --debug                   ����������־
# --plugin-base-dir <DIR>   ָ�����·����Ĭ��Ϊ"./plugins"
```

### �����ļ�

�����ļ�`appsettings.json`����������Ҫ���ã�

```json
{
  "SystemConfig": {
    "LibrarianUrl": "http://librarian",                 // Librarian�����ַ
    "LibrarianRefreshToken": "",                        // ��֤����
    "DbPath": "./sentinel.db",                          // ���ݿ�·��
    "PluginBaseDir": "./plugins",                       // ���Ŀ¼
    "LibraryConfigs": [                                 // �������б�
      {
        "PluginName": "SingleFile",                     // �������
        "DownloadBasePath": "/library",                 // ����·��ǰ׺
        "PluginConfig": {                               // �������
          "LibraryName": "Library1",                    // ������
          "LibraryFolder": "/path/to/library1",         // ���ļ���·��
          "ChunkSizeBytes": "67108864",                 // �ֿ��С (64MB)
          "ForceCalcDigest": "false"                    // �Ƿ�ǿ�Ƽ���ժҪ
        }
      }
    ],
    "LibraryScanIntervalMinutes": 1440                  // ɨ���������ӣ�
  },
  "SentinelConfig": {
    "Hostnames": ["https://dl1.example.com:1234"],      // URL�б�
    "NeedToken": "true",                                // �Ƿ���Ҫ����
    "GetTokenUrlPath": "/get_token",                    // ��ȡ����·��
    "DownloadFileUrlPath": "/download_file"             // �����ļ�·��
  }
}
```

����`PluginConfig`���֣����ݲ�����Ͳ�ͬ��������Ҳ��ͬ
- ���в������
  - `LibraryName`��������
  - `LibraryFolder`�����ļ���·��
  - `ChunkSizeBytes`���ֿ��С���ֽڣ�
  - `ForceCalcDigest`��ɨ��ʱ�Ƿ�ǿ�Ƽ���ժҪ
- `SingleFile`�������
  - ���й��в������
- `SubFolder`�������
  - ���в������
  - `MinDepth`����С��ȣ���СΪ1
  - `ScanPolicy`��ɨ�����
    - `UntilAnyFile`��ֱ�������ļ����ļ���Ϊһ��Binary��Ĭ�ϣ�
    - `UntilNoFolder`��ֱ��ֻ���ļ����ļ���Ϊһ��Binary
- `PythonPluginLoader`�������
  - ���в������
  - `PythonScriptPath`��Python�ű�·��
  - `PythonClassName`��Python����
  - `PythonScriptCustomConfig`���Զ������ã�Ϊһ���ֵ�

### OpenResty����

�ṩ��OpenResty����ʾ���������������ļ�������֤�ͷ��ʿ��ƣ�

- �����ļ���`config.json`������Կ��Ŀ¼ӳ�䣬��Sentinel����
- Lua�ű���`sentinel_config.lua`�������ã�Ӳ����`config.json`λ�ã�
- Nginx���ã�������ʼ���ű������ƹ����λ������

#### ������������

1. �ͻ��������������� (`/get_token`)
2. OpenResty��֤��������JWT����
3. �ͻ���ʹ�����������ļ� (`/download_file`)
4. OpenResty��֤���ơ�����·�����ṩ�ļ�����
5. Ӧ���������ƺ��������Ʒ�ֹ����

## �������

Sentinel-CSharp֧�ֱ�д�Զ�������

- C#�����ʵ��IPlugin�ӿ�
- Python������̳�PluginBase��ͨ��PythonPluginLoader����
