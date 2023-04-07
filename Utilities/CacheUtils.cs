using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Onset_Serialization.Utilities
{
    public class CacheUtils
    {
        private IDictionary<String, Object> _cacheDict;
        private String _path;

        public CacheUtils(String name)
        {
            _path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                $"_OnsetApp__{name}.tmp");
            _cacheDict = GetFromFile<Dictionary<String, Object>>(_path);
        }

        public CacheUtils(FileInfo file)
        {
            _path = file.FullName;
            _cacheDict = GetFromFile<Dictionary<String, Object>>(_path);
        }

        public void SetCache(String key, Object value)
        {
            if (_cacheDict.ContainsKey(key))
            {
                _cacheDict[key] = value;
            }
            else
            {
                _cacheDict.Add(key, value);
            }
        }

        public Object GetCache(String key)
        {
            try
            {
                return _cacheDict[key];
            }
            catch { }
            return null;
        }

        public void Save()
        {
            SaveToFile(_path, _cacheDict);
        }

        private void SaveToFile<Object>(String filePath, Object dictionary)
        {
            try // try to serialize the collection to a file
            {
                using (var stream = File.Open(filePath, FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, dictionary);
                }
            }
            catch (IOException)
            {
            }
        }

        private Object GetFromFile<Object>(String filePath) where Object : new()
        {
            Object ret = CreateInstance<Object>();
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    ret = (Object)bin.Deserialize(stream);
                }
            }
            catch (IOException)
            {
            }
            return ret;
        }

        private Object CreateInstance<Object>() where Object : new()
        {
            return (Object)Activator.CreateInstance(typeof(Object));
        }
    }
}
