using System.Collections.Generic;
using Chronolog.Domain;
using System.Linq;
using UnityEngine;
using System.IO;
using System;

namespace Chronolog.Persistence
{
    public sealed class JsonJournalRecordRepository : IJournalRecordRepository
    {
        private const int CurrentStorageVersion = 1;
        private readonly string storageFilePath;

        public JsonJournalRecordRepository(string storageFilePath)
        {
            if (string.IsNullOrWhiteSpace(storageFilePath))
                throw new ArgumentException("Storage file path is required.", nameof(storageFilePath));

            this.storageFilePath = storageFilePath;
        }

        public IReadOnlyList<JournalRecord> GetAll()
        {
            if (!File.Exists(storageFilePath))
                return Array.Empty<JournalRecord>();

            var document = ReadDocument();
            return document.records
                .Select(record => record.ToRecord())
                .OrderByDescending(record => record.CreatedAtUtc)
                .ToArray();
        }

        public void Save(JournalRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var document = File.Exists(storageFilePath) ? ReadDocument() : new JournalRecordStoreDocument();
            var replacement = JournalRecordDocument.FromRecord(record);
            var existingIndex = document.records.FindIndex(item => item.id == replacement.id);

            if (existingIndex >= 0)
                document.records[existingIndex] = replacement;
            else
                document.records.Add(replacement);

            WriteDocument(document);
        }

        public void Delete(Guid recordId)
        {
            if (!File.Exists(storageFilePath))
                return;

            var document = ReadDocument();
            document.records.RemoveAll(record => record.id == recordId.ToString("D"));
            WriteDocument(document);
        }

        private JournalRecordStoreDocument ReadDocument()
        {
            var json = File.ReadAllText(storageFilePath);
            var document = JsonUtility.FromJson<JournalRecordStoreDocument>(json);

            if (document == null || document.version != CurrentStorageVersion || document.records == null)
                throw new InvalidDataException("Journal storage has an unsupported format.");

            return document;
        }

        private void WriteDocument(JournalRecordStoreDocument document)
        {
            document.version = CurrentStorageVersion;
            var directoryPath = Path.GetDirectoryName(storageFilePath);
            
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var temporaryFilePath = storageFilePath + ".tmp";
            File.WriteAllText(temporaryFilePath, JsonUtility.ToJson(document, true));

            if (File.Exists(storageFilePath))
                File.Replace(temporaryFilePath, storageFilePath, null);
            else
                File.Move(temporaryFilePath, storageFilePath);
        }
    }
}