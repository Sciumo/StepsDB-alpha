﻿// Copyright (C) 2008, by David W. Jeske
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Bend
{

    [TestFixture]
    public class TestRecordClasses
    {
        [Test]
        public void Test00RecordKeyEquality() {
            RecordKey key1 = new RecordKey().appendParsedKey("test/1");
            RecordKey key2 = new RecordKey().appendParsedKey("test/1");

            
            Assert.AreEqual(key1, key2, "object equal");
            Assert.AreEqual(true, key1.Equals(key2), "key1.Equals(key2)");
        }

        [Test]
        public void Test01RecordKey() {
            String[] parts1 = { "test", "test2", "blah" };

            RecordKey key = new RecordKey();
            key.appendKeyParts(parts1);
            byte[] data = key.encode();

            // decode
            RecordKey key2 = new RecordKey(data);

            // verify tostring matches
            Assert.AreEqual(key.ToString(), key2.ToString());

            // verify comparison
            Assert.AreEqual(0, key.CompareTo(key2));

            // verify individual parts            
        }
        [Test]
        public void Test02RecordSort() {
            String[] parts1 = { "test", "test2", "blah" };
            String[] parts2 = { "test", "test3", "blah" }; // > parts 1
            String[] parts3 = { "test", "test2a", "blah" }; // > parts 1 (testing per-segment sorting order!)

            RecordKey key1 = new RecordKey();
            key1.appendKeyParts(parts1);

            RecordKey key2 = new RecordKey();
            key2.appendKeyParts(parts2);

            RecordKey key3 = new RecordKey();
            key3.appendKeyParts(parts3);

            // key2 > key1
            Assert.AreEqual(1, key2.CompareTo(key1));
            Assert.AreEqual(-1, key1.CompareTo(key2));

            // key3 > key1
            Assert.AreEqual(1, key3.CompareTo(key1));
            Assert.AreEqual(-1, key1.CompareTo(key3));

        }

        [Test]
        public void Test03RecordDataAssembly() {
            RecordData data = new RecordData(RecordDataState.NOT_PROVIDED, new RecordKey());
            Assert.AreEqual("", data.ToString());
            RecordUpdateResult result;


            result = data.applyUpdate(new RecordUpdate(RecordUpdateTypes.NONE, ""));
            Assert.AreEqual("", data.ToString());
            Assert.AreEqual(result, RecordUpdateResult.SUCCESS);

            result = data.applyUpdate(new RecordUpdate(RecordUpdateTypes.FULL, "1"));
            Assert.AreEqual("1", data.ToString());
            Assert.AreEqual(result, RecordUpdateResult.FINAL);

            // if we already have a full update, it should be an error
            {
                bool err = false;
                try { data.applyUpdate(new RecordUpdate(RecordUpdateTypes.FULL, "2")); } catch { err = true; }
                Assert.AreEqual(true, err);
            }
            {
                bool err = false;
                try { data.applyUpdate(new RecordUpdate(RecordUpdateTypes.DELETION_TOMBSTONE, "")); } catch { err = true; }
                Assert.AreEqual(true, err);
            }
            Assert.AreEqual("1", data.ToString());

        }

        [Test]
        public void Test04RecordTombstones() {
            RecordData data = new RecordData(RecordDataState.NOT_PROVIDED, new RecordKey());
            Assert.AreEqual("", data.ToString());

            RecordUpdateResult result = data.applyUpdate(new RecordUpdate(RecordUpdateTypes.DELETION_TOMBSTONE, ""));
            Assert.AreEqual("", data.ToString());
            Assert.AreEqual(result, RecordUpdateResult.FINAL);

            bool err = false;
            try {
                data.applyUpdate(new RecordUpdate(RecordUpdateTypes.FULL, "2"));
                data.applyUpdate(new RecordUpdate(RecordUpdateTypes.DELETION_TOMBSTONE, ""));
            } catch {
                err = true;
            }
            Assert.AreEqual(err, true);

        }

        [Test]
        public void Test05RecordPartialUpdate() {
            RecordData data = new RecordData(RecordDataState.NOT_PROVIDED, new RecordKey());
            Assert.AreEqual("", data.ToString());

            // TODO: what the heck is a partial update?!?!?
            // result = data.applyUpdate(new RecordUpdate(RecordUpdateTypes.PARTIAL, "1"));
            Assert.Fail("partial update not implemented yet");
        }

        [Test]
        public void Test06RecordKeyDelimiterEscape() {
            string DELIM = new String(RecordKey.DELIMITER, 1);


            RecordKey key1 = new RecordKey();
            key1.appendKeyParts("1", "2", "3");
            Assert.AreEqual(3, key1.numParts());
            RecordKey dkey1 = new RecordKey(key1.encode());
            Assert.AreEqual(3, dkey1.numParts(), "dkey1 delimiter decode");

            RecordKey key2 = new RecordKey();
            key2.appendKeyPart("1" + DELIM + "2" + DELIM + "3");
            Assert.AreEqual(1, key2.numParts());
            RecordKey dkey2 = new RecordKey(key2.encode());
            Assert.AreEqual(1, dkey2.numParts(), "dkey2 delimiter decode");

            // key2 > key1
            Assert.AreEqual(1, key2.CompareTo(key1));
            Assert.AreEqual(-1, key1.CompareTo(key2));

        }


    }

}