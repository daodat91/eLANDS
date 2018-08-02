using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoMap
{
    public class MapperConfig
    {
        public static MapperTable CreateMap<T>(IEnumerable<string> columnsName)
        {
            return new MapperTable(Utils._GetMapFieldObject<T>(columnsName));
        }
        public static MapperTable CreateMap<T>(DataColumnCollection columns)
        {
            return new MapperTable(Utils._GetMapFieldObject<T>(columns));
        }
        public static MapperObject CreateMap<TSource, TResult>(bool fieldNameIsSame = true) where TResult : new()
        {
            return new MapperObject(Utils._GetMapPropertyObject<TSource, TResult>(fieldNameIsSame));
        }
    }

    public class Utils
    {
        /// <summary>
        /// Get Mapping column và Object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnsName"></param>
        /// <returns></returns>
        public static IEnumerable<MapFieldTable> _GetMapFieldObject<T>(IEnumerable<string> columnsName)
        {
            Dictionary<string, string> dictColumnName = new Dictionary<string, string>();
            List<MapFieldTable> lstFieldName = new List<MapFieldTable>();
            foreach (string item2 in columnsName)
            {
                dictColumnName.Add(item2.ToUpper(), item2);
            }
            foreach (var propSource in typeof(T).GetProperties())
            {
                var fieldName = propSource.Name.ToUpper();
                if (dictColumnName.ContainsKey(fieldName))
                {
                    lstFieldName.Add(new MapFieldTable { ColumnName = dictColumnName[fieldName], Property = propSource });
                }
            }

            return lstFieldName;
        }
        /// <summary>
        /// Get Mapping column và Object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static IEnumerable<MapFieldTable> _GetMapFieldObject<T>(DataColumnCollection columns)
        {
            Dictionary<string, string> dictColumnName = new Dictionary<string, string>();
            List<MapFieldTable> lstFieldName = new List<MapFieldTable>();
            foreach (DataColumn item2 in columns)
            {
                dictColumnName.Add(item2.ColumnName.ToUpper(), item2.ColumnName);
            }
            foreach (var propSource in typeof(T).GetProperties())
            {
                var fieldName = propSource.Name.ToUpper();
                if (dictColumnName.ContainsKey(fieldName))
                {
                    lstFieldName.Add(new MapFieldTable { ColumnName = dictColumnName[fieldName], Property = propSource });
                }
            }

            return lstFieldName;
        }
        /// <summary>
        /// Get mapping thuộc tính của 2 object
        /// </summary>
        /// <typeparam name="TSource">Object nguồn</typeparam>
        /// <typeparam name="TResult">Object đích</typeparam>
        /// <param name="fieldNameIsSame">fieldNameIsSame=true: FieldName phân biệt hoa, thường</param>
        /// <returns></returns>
        public static IEnumerable<MapPropertyObject> _GetMapPropertyObject<TSource, TResult>(bool fieldNameIsSame = true) where TResult : new()
        {
            List<MapPropertyObject> lstMap = new List<MapPropertyObject>();
            if (fieldNameIsSame)
            {
                foreach (var propResult in typeof(TResult).GetProperties())
                {
                    var propSource = typeof(TSource).GetProperty(propResult.Name);
                    if (propSource != null)
                        lstMap.Add(new MapPropertyObject { PropertyResult = propResult, PropertySource = propSource });
                }
            }
            else
            {
                var propResults = typeof(TResult).GetProperties();
                foreach (var propSource in typeof(TSource).GetProperties())
                {
                    foreach (var propResult in propResults)
                    {
                        if (propSource.Name.ToUpper() == propResult.Name.ToUpper())
                        {
                            lstMap.Add(new MapPropertyObject { PropertyResult = propResult, PropertySource = propSource });
                        }
                    }
                }
            }

            return lstMap;
        }
    }

    public class MapperTable
    {
        private IEnumerable<MapFieldTable> _mapColumnTables;
        public MapperTable(IEnumerable<MapFieldTable> value)
        {
            _mapColumnTables = value;
        }

        public TSource Map<TSource>(DataRow dr) where TSource : new()
        {
            TSource source = new TSource();
            foreach (var mapField in _mapColumnTables)
            {
                object objValue = dr[mapField.ColumnName];
                if (objValue != DBNull.Value)
                    mapField.Property.SetValue(source, objValue);
            }

            return source;
        }
        public IEnumerable<TSource> Map<TSource>(DataTable dt) where TSource : new()
        {
            List<TSource> lst = new List<TSource>();
            foreach (DataRow item in dt.Rows)
            {
                TSource source = new TSource();
                foreach (var mapField in _mapColumnTables)
                {
                    object objValue = item[mapField.ColumnName];
                    if (objValue != DBNull.Value)
                        mapField.Property.SetValue(source, objValue);
                }

                lst.Add(source);
            }

            return lst;
        }
        public static IEnumerable<TSource> __Map<TSource>(DataTable dt) where TSource : new()
        {
            List<TSource> lst = new List<TSource>();
            var lstFieldName = Utils._GetMapFieldObject<TSource>(dt.Columns);

            foreach (DataRow item in dt.Rows)
            {
                TSource source = new TSource();
                foreach (var mapField in lstFieldName)
                {
                    object objValue = item[mapField.ColumnName];
                    if (objValue != DBNull.Value)
                        mapField.Property.SetValue(source, objValue);
                }

                lst.Add(source);
            }

            return lst;
        }
        public static TSource __Map<TSource>(DataRow dr) where TSource : new()
        {
            var lstFieldName = Utils._GetMapFieldObject<TSource>(dr.Table.Columns);
            TSource source = new TSource();
            foreach (var mapField in lstFieldName)
            {
                object objValue = dr[mapField.ColumnName];
                if (objValue != DBNull.Value)
                    mapField.Property.SetValue(source, objValue);
            }

            return source;
        }

        public static DataTable __Map<TSource>(IEnumerable<TSource> values) where TSource : new()
        {
            var propResults = typeof(TSource).GetProperties();
            DataTable dt = new DataTable();
            foreach (var item in propResults)
            {
                var prop = item.PropertyType;
                dt.Columns.Add(new DataColumn(item.Name, Nullable.GetUnderlyingType(prop) ?? prop));
            }

            foreach (var item in values)
            {
                DataRow dr = dt.NewRow();
                TSource source = new TSource();
                foreach (var prop in propResults)
                {
                    object objValue = prop.GetValue(item);
                    if (objValue != null)
                        dr[prop.Name] = objValue;
                }

                dt.Rows.Add(dr);
            }

            return dt;
        }
    }

    public class MapperObject
    {
        private IEnumerable<MapPropertyObject> _mapPropertys;

        public MapperObject(IEnumerable<MapPropertyObject> value)
        {
            _mapPropertys = value;
        }
        public TResult Map<TSource, TResult>(TSource Source) where TResult : new()
        {
            List<TResult> lstData = new List<TResult>();

            TResult result = new TResult();
            foreach (var itemMap in _mapPropertys)
            {
                var value = itemMap.PropertySource.GetValue(Source);
                itemMap.PropertyResult.SetValue(result, value);
            }

            return result;
        }
        public IEnumerable<TResult> Map<TSource, TResult>(IEnumerable<TSource> Source) where TResult : new()
        {
            List<TResult> lstData = new List<TResult>();
            var lstMap = Utils._GetMapPropertyObject<TSource, TResult>();
            foreach (var item in Source)
            {
                TResult result = new TResult();
                foreach (var itemMap in _mapPropertys)
                {
                    var value = itemMap.PropertySource.GetValue(item);
                    itemMap.PropertyResult.SetValue(result, value);
                }
                lstData.Add(result);
            }

            return lstData;
        }

        public static TResult __Map<TSource, TResult>(TSource Source, bool fieldNameIsSame = true) where TResult : new()
        {
            var lstMap = Utils._GetMapPropertyObject<TSource, TResult>(fieldNameIsSame);
            TResult result = new TResult();
            foreach (var itemMap in lstMap)
            {
                var value = itemMap.PropertySource.GetValue(Source);
                itemMap.PropertyResult.SetValue(result, value);
            }

            return result;
        }
        public static IEnumerable<TResult> __Map<TSource, TResult>(IEnumerable<TSource> Source, bool fieldNameIsSame = true) where TResult : new()
        {
            List<TResult> lstData = new List<TResult>();
            var lstMap = Utils._GetMapPropertyObject<TSource, TResult>(fieldNameIsSame);
            foreach (var item in Source)
            {
                TResult result = new TResult();
                foreach (var itemMap in lstMap)
                {
                    var value = itemMap.PropertySource.GetValue(item);
                    itemMap.PropertyResult.SetValue(result, value);
                }
                lstData.Add(result);
            }

            return lstData;
        }
    }

    public class MapFieldTable
    {
        public string ColumnName { get; set; }
        public PropertyInfo Property { get; set; }
    }

    public class MapPropertyObject
    {
        public PropertyInfo PropertySource { get; set; }
        public PropertyInfo PropertyResult { get; set; }
    }
}
