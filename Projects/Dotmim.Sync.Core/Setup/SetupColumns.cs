﻿using Dotmim.Sync.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Dotmim.Sync
{
    /// <summary>
    /// List of columns within a table, to add to the sync process 
    /// </summary>
    [CollectionDataContract(Name = "cols", ItemName = "col"), Serializable]
    public class SetupColumns : ICollection<string>, IList<string>
    {
        /// <summary>
        /// Exposing the InnerCollection for serialization purpose
        /// </summary>
        [DataMember(Name = "c", IsRequired = true, Order = 1)]
        public Collection<string> InnerCollection = new Collection<string>();

        public SetupColumns() { }

        /// <summary>
        /// Add a new column to the list of columns to be added to the sync
        /// </summary>
        public void Add(string columnName)
        {
            var parserColumnName = ParserName.Parse(columnName);
            var columnNameNormalized = parserColumnName.ObjectName;

            if (InnerCollection.Any(c => string.Equals(c, columnName, SyncGlobalization.DataSourceStringComparison)))
                throw new Exception($"Column name {columnNameNormalized} already exists in the table");

            InnerCollection.Add(columnNameNormalized);
        }

        /// <summary>
        /// Add a range of columns to the sync process setup
        /// </summary>
        public void AddRange(IEnumerable<string> columnsName)
        {
            foreach (var columnName in columnsName)
                this.Add(columnName);
        }

        /// <summary>
        /// Add a range of columns to the sync process setup
        /// </summary>
        public void AddRange(params string[] columnsName)
        {
            foreach (var columnName in columnsName)
                this.Add(columnName);
        }


        /// <summary>
        /// Clear all columns
        /// </summary>
        public void Clear() => this.InnerCollection.Clear();


        /// <summary>
        /// Get a Column by its name
        /// </summary>
        public string this[string columnName]
            => InnerCollection.FirstOrDefault(c => string.Equals(c, columnName, SyncGlobalization.DataSourceStringComparison));


        public string this[int index] => InnerCollection[index];
        public int Count => InnerCollection.Count;
        public bool IsReadOnly => false;
        string IList<string>.this[int index] { get => this.InnerCollection[index]; set => this.InnerCollection[index] = value; }
        public bool Remove(string item) => InnerCollection.Remove(item);
        public bool Contains(string item) => InnerCollection.Any(c => string.Equals(c, item, SyncGlobalization.DataSourceStringComparison));
        public void CopyTo(string[] array, int arrayIndex) => InnerCollection.CopyTo(array, arrayIndex);
        public int IndexOf(string item) => InnerCollection.IndexOf(item);
        public void RemoveAt(int index) => InnerCollection.RemoveAt(index);
        public override string ToString() => this.InnerCollection.Count.ToString();
        public void Insert(int index, string item) => this.InnerCollection.Insert(index, item);
        public IEnumerator<string> GetEnumerator() => InnerCollection.GetEnumerator();
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => this.InnerCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.InnerCollection.GetEnumerator();

    }

}
