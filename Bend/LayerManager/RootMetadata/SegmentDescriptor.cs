﻿// Copyright (C) 2008, by David W. Jeske
// All Rights Reserved.


using System;
using System.Collections.Generic;

using System.Diagnostics;


namespace Bend {

    // currently...

    // .ROOT/GEN/000/</> -> addr:length

    public class SegmentDescriptor : IComparable<SegmentDescriptor> {
        public readonly RecordKey record_key;
        public readonly uint generation;
        public readonly RecordKey start_key;
        public readonly RecordKey end_key;

        public SegmentDescriptor(RecordKey key) {
            RecordKey expected_prefix = new RecordKey().appendParsedKey(".ROOT/GEN");
            if (!key.isSubkeyOf(expected_prefix)) {
                throw new Exception("can't decode key as segment descriptor: " + key.ToString());
            }
            record_key = key;

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

            generation = (uint)Lsd.lsdToNumber(enc.GetBytes(key.key_parts[2]));
            start_key = new RecordKey().appendParsedKey(key.key_parts[3]);
            end_key = new RecordKey().appendParsedKey(key.key_parts[4]);

            {
                var testkey = new RecordKey().appendParsedKey(".ROOT/VARS/NUMGENERATIONS");
                if (start_key.Equals(testkey)) {
                    System.Console.WriteLine("%%%%%% GEN started with numgenerations ..." + this);
                    System.Console.WriteLine(WhoCalls.StackTrace());
                }
            }

        }

        public int CompareTo(SegmentDescriptor target) {
            
            switch(this.generation.CompareTo(target.generation)) {
                case -1: 
                    return -1;
                case 1:
                    return 1;
                case 0:
                    switch(this.start_key.CompareTo(target.start_key)) {
                        case -1:
                            return -1;
                        case 1:
                            return 1;
                        case 0:
                            return this.end_key.CompareTo(target.end_key);
                    
                    }
                    break;
            }
            throw new Exception("invalid values in SegmentDescriptor.CompareTo()");
        }

        public bool keyrangeOverlapsWith(SegmentDescriptor target) {
            return this.keyrangeOverlapsWith(target.start_key, target.end_key);
        }
        public bool keyrangeOverlapsWith(RecordKey t_start_key, RecordKey t_end_key) {
            // ignore generation
            if ((this.start_key.CompareTo(t_end_key) > 0) ||
                (this.end_key.CompareTo(t_start_key) < 0)) {
                return false;
            }
            return true;
        }

        public SegmentDescriptor(uint generation, RecordKey start_key, RecordKey end_key) {

            RecordKey genkey = new RecordKey()
                .appendParsedKey(".ROOT/GEN")
                .appendKeyPart(Lsd.numberToLsd((int)generation, RangemapManager.GEN_LSD_PAD))
                .appendKeyPart(start_key.encode())
                .appendKeyPart(end_key.encode());

            this.record_key = genkey;
            this.generation = generation;
            this.start_key = start_key;
            this.end_key = end_key;


            // double check that the encode/decode is reversible
            {
                SegmentDescriptor check_sdesc = new SegmentDescriptor(this.record_key);

                if (this.CompareTo(check_sdesc) != 0) {
                    Console.WriteLine("start key: hext {1} - {0}",start_key,Lsd.ToHexString(start_key.encode()));
                    Console.WriteLine("end_key: hex {1} - {0}",end_key,Lsd.ToHexString(end_key.encode()));

                    throw new Exception(
                        String.Format("mapGenerationToRegion: segment descriptor pack/unpack error ({0}) decoded to ({1})",
                        this,check_sdesc));
                }
            }

        }

        public override string ToString() {
            return "SegmentDescriptor{" + generation + ":" + start_key + ":" +
                end_key + "}";
        }
        public ISortedSegment getSegment(RangemapManager rmm) {
            RecordKey found_key = new RecordKey();
            RecordData found_record = new RecordData(RecordDataState.NOT_PROVIDED, found_key);
            if (rmm.getNextRecord(this.record_key, ref found_key, ref found_record, true) == GetStatus.PRESENT) {
                if (this.record_key.Equals(found_key) && found_record.State == RecordDataState.FULL) {
                    return rmm.getSegmentFromMetadata(found_record);
                }
                if (found_record.State == RecordDataState.DELETED) {
                    throw new Exception("Segment{" + this.ToString() + "}.getSegment() returned tombstone");
                }
            }
            throw new Exception("Could not load Segment from SegmentDescriptor: " + this);
        }

    }

}


namespace BendTests {
    using Bend;

    using NUnit.Framework;

    public partial class A02_SegmentDescriptorTests {
        [Test]
        public void T01_SegmentDescriptorBasic() {
            var a = new SegmentDescriptor(0,new RecordKey().appendParsedKey("AAAA"),new RecordKey().appendParsedKey("ZZZZ"));
            var f = new SegmentDescriptor(1,new RecordKey().appendParsedKey("AAAA"),new RecordKey().appendParsedKey("ZZZZ"));

            Assert.AreEqual(-1, a.CompareTo(f));

            var b1 = new SegmentDescriptor(0,
                                new RecordKey().appendParsedKey("test/rnd/306608504"),
                                new RecordKey().appendParsedKey("test/rnd/653566822"));
            var b2 = new SegmentDescriptor(0, 
                                new RecordKey().appendParsedKey("test/rnd/328202073"), 
                                new RecordKey().appendParsedKey("test/rnd/669936319"));
            var b3 = new SegmentDescriptor(0,
                                new RecordKey().appendParsedKey("test/rnd/328202073"),
                                new RecordKey().appendParsedKey("test/rnd/996219212"));
            var b4 = new SegmentDescriptor(0,
                                new RecordKey().appendParsedKey("test/rnd/728202073"),
                                new RecordKey().appendParsedKey("test/rnd/996219212"));



            Assert.AreEqual(-1, b1.CompareTo(b2));
            Assert.AreEqual(-1, b2.CompareTo(b3));


            // test overlap computation
            Assert.AreEqual(true, a.keyrangeOverlapsWith(f));
            Assert.AreEqual(true, b1.keyrangeOverlapsWith(b2));
            Assert.AreEqual(true, b3.keyrangeOverlapsWith(b2));
            Assert.AreEqual(false, b2.keyrangeOverlapsWith(b4));
        }

        [Test]
        public void T02_DescriptorOverlapTests() {
            // BUG how did these overlappipng blocks ever get created?!?!? 
            // gen2 start(test/rnd/1988483319) end(test/rnd/254306374)
            // gen2 start(test/rnd/254612715)  end(test/rnd/678413856)
            // gen2 start(test/rnd/272911872)  end(test/rnd/464592052)

            var b1 = new SegmentDescriptor(0,
                                new RecordKey().appendKeyPart(new RecordKey().appendParsedKey("test/rnd/1988483319")),
                                new RecordKey().appendKeyPart(new RecordKey().appendParsedKey("test/rnd/254306374")));

            var b2 = new SegmentDescriptor(0,
                                new RecordKey().appendKeyPart(new RecordKey().appendParsedKey("test/rnd/254612715")),
                                new RecordKey().appendKeyPart(new RecordKey().appendParsedKey("test/rnd/678413856")));

            var b3 = new SegmentDescriptor(0,
                                new RecordKey().appendKeyPart(new RecordKey().appendParsedKey("test/rnd/1988483319")),
                                new RecordKey().appendKeyPart(new RecordKey().appendParsedKey("test/rnd/464592052")));

            Assert.AreEqual(true, b1.keyrangeOverlapsWith(b2));
            Assert.AreEqual(true, b1.keyrangeOverlapsWith(b3));
            Assert.AreEqual(true, b2.keyrangeOverlapsWith(b3));

        }

        [Test]
        public void T05_DescriptorEncodeDecodeBugTests() {
            /* 
             * .zdata.index.\/.c:\EmailTest\Data\saved_mail_2002:1104.131
             * 
             *  46 (122 100 97 116 97) 
             *  47 (105 110 100 101 120) 
             *  47 (92 43 0)   <-- culpret
             *  47 (99 58 92 69 109 97 105 108 84 101 115 116 92 68 97 116 97 92 115 97 118 101 100 95 109 
             *  97 105 108 95 50 48 48 50 58 49 49 48 52 47 49 51 49)
             */

            byte[] test_part = { 92, 43, 0 };           
            RecordKey test = new RecordKey()
                .appendKeyPart(".zdata")
                .appendKeyPart("index")
                .appendKeyPart(test_part)
                .appendKeyPart(@"c:\EmailTest\Data\saved_mail_2002:1104.131");
            
            SegmentDescriptor sdesc = new SegmentDescriptor(0,test,test);

            Assert.AreEqual(sdesc, new SegmentDescriptor(sdesc.record_key), "segment descriptor encode/decode problem");
        }
            
    }

}