﻿// Copyright (C) 2008, by David W. Jeske
// All Rights Reserved.


using System;
using System.Collections.Generic;

using System.Diagnostics;


namespace Bend
{

    // RANGEs are represented with an implicit prefix '='. This allows special endpoint markers:
    // "<" - the key before all keys
    // "=KEYDATA" - keys after and including "KEYDATA"
    // ">" - the key after all keys


    // .ROOT/VARS/NUMGENERATIONS -> 1
    // .ROOT/GEN/(gen #:3)/(start key)/(end key) -> (segment metadata)
    // .ROOT/GEN/000/</> -> addr:length

    public class RangemapManager
    {
        LayerManager store;
        int num_generations;
        private static int GEN_LSD_PAD = 3;


        // TODO: FIXME: this is a hacky cache... the segmentreaders sitting inside
        //   use a single FileStream. If you have multiple threads calling them, 
        //   chaos will ensue because of the shared seek pointer. 
        Dictionary<RecordKey, SegmentReader> disk_segment_cache; 

        public RangemapManager(LayerManager store) {
            this.store = store;
            // get the current number of generations

            disk_segment_cache = new Dictionary<RecordKey, SegmentReader>();

            RecordUpdate update;
            if (store.workingSegment.getRecordUpdate(new RecordKey().appendParsedKey(".ROOT/VARS/NUMGENERATIONS"),
                out update) == GetStatus.MISSING) {
                throw new Exception("RangemapManager can't init without NUMGENERATIONS");
            }
            num_generations = (int)Lsd.lsdToNumber(update.data);
        }
        public static void Init(LayerManager store) {
            // setup "zero" initial generations
            store.setValue(new RecordKey().appendParsedKey(".ROOT/VARS/NUMGENERATIONS"),
                RecordUpdate.WithPayload(0.ToString())); // TODO: this should be a var-enc number
        }


        internal SegmentReader segmentReaderFromRow(KeyValuePair<RecordKey, RecordUpdate> segmentrow) {
            return segmentReaderFromRow(segmentrow.Key, segmentrow.Value);
        }

        internal SegmentReader segmentReaderFromRow(RecordKey key, RecordUpdate update) {
            lock (disk_segment_cache) {
                try {
                    return disk_segment_cache[key];
                } catch (KeyNotFoundException) {
                    SegmentReader next_seg = getSegmentFromMetadataBytes(update.data);
                    disk_segment_cache[key] = next_seg;
                    return next_seg;
                }
            }
        }        


        public void mapGenerationToRegion(LayerManager.WriteGroup tx, int gen_number, RecordKey start_key, RecordKey end_key, IRegion region) {

            RecordKey key = makeGenerationKey(gen_number, start_key, end_key);
                
            // TODO: pack the metdata record <addr>:<size>
            // String segmetadata = String.Format("{0}:{1}", region.getStartAddress(), region.getSize());            
            String seg_metadata = "" + region.getStartAddress();
            tx.setValue(key, RecordUpdate.WithPayload(seg_metadata));

        }

        private RecordKey makeGenerationKey(int gen_number, RecordKey start_key, RecordKey end_key) {
            RecordKey genkey = new RecordKey()
                .appendParsedKey(".ROOT/GEN")
                .appendKeyPart(Lsd.numberToLsd(gen_number, GEN_LSD_PAD))
                .appendKeyPart(start_key.encode())
                .appendKeyPart(end_key.encode());                
           
            return genkey;

        }

        public void clearSegmentCacheHack() {            
            lock (disk_segment_cache) {
                disk_segment_cache = new Dictionary<RecordKey, SegmentReader>();
            }
            System.Console.WriteLine("*** clearSegmentCacheHack() ***");
            // TODO: see unmapGeneration(). This should go away when a transaction apply which
            //  .. touches a rangemap row automagically causes an invalidation of the segment cache            
        }

        public void unmapSegment(LayerManager.WriteGroup tx, RecordKey key, RecordData data) {
            // TODO: how do we assure that existing read operations flush and reload all segments?          

            lock (disk_segment_cache) {
                // clear the entry from the cache
                // TODO: fix this so it works when we fix setValue...
                //   ... technically this only works right now because setValue applies immediately.
                //   ... if it actually applied when the TX commits like it's supposed to, there would
                //   ... be a race condition here
                try {
                    disk_segment_cache.Remove(key);
                }
                catch (KeyNotFoundException) {
                    // not being in ths cache is okay
                }
            }
            tx.setValue(key, RecordUpdate.DeletionTombstone());

            // we can't really do this because the file is still open
            // store.regionmgr.disposeRegionAddr(unpackRegionAddr(data.data));            
        }
       
        public void setGenerationCountToZeroHack() {                       
            int highest_valid_gen = 0;
            if (highest_valid_gen + 1 < num_generations) {
                num_generations = highest_valid_gen + 1;
                store.setValue(new RecordKey().appendParsedKey(".ROOT/VARS/NUMGENERATIONS"),
                    RecordUpdate.WithPayload(num_generations.ToString()));
            }
        }
       
        public int allocNewGeneration(LayerManager.WriteGroup tx) {
            // allocate a new generation number
            int newgen = num_generations;
            num_generations++;
    
            // TODO: write the new generation count, and the rangemap entry for the generation

            tx.setValue(new RecordKey().appendParsedKey(".ROOT/VARS/NUMGENERATIONS"),
                RecordUpdate.WithPayload(num_generations.ToString()));

            return newgen;
        }
        private uint unpackRegionAddr(byte[] data) {
            // TODO:unpack the update data when we change it to "<addr>:<length>"
            return (uint)Lsd.lsdToNumber(data);
        }
        private SegmentReader getSegmentFromMetadataBytes(byte[] data) {
            // we now have a pointer to a segment addres for GEN<max>
            uint region_addr = unpackRegionAddr(data);

            System.Console.WriteLine("-- open SegmentReader {0}", region_addr);
            IRegion region = store.regionmgr.readRegionAddrNonExcl(region_addr);
            SegmentReader sr = new SegmentReader(region);
            return sr;

        }

        public ISortedSegment getSegmentFromMetadata(RecordData data) {
            return getSegmentFromMetadataBytes(data.data);
        }

        public ISortedSegment getSegmentFromMetadata(RecordUpdate update) {
            return getSegmentFromMetadataBytes(update.data);
        }

        public int genCount() {
            return num_generations;
        }

        // ------------[ public segmentWalkForKey ] --------------

        public RecordUpdateResult segmentWalkForKey_OLD(
            RecordKey key,
            ISortedSegment curseg,
            ref RecordData record) {

            HashSet<int> handledGenerations = new HashSet<int>();
            return this.INTERNAL_segmentWalkForKey_OLD(
                key, curseg, handledGenerations, num_generations, ref record);


        }

        public GetStatus getNextRecord(RecordKey lowkey, ref RecordKey key, ref RecordData record, bool equal_ok) {

            BDSkipList<RecordKey, RecordData> handledIndexRecords = new BDSkipList<RecordKey,RecordData>();
            BDSkipList<RecordKey, RecordData> recordsBeingAssembled = new BDSkipList<RecordKey, RecordData>();


            SegmentMemoryBuilder[] layers;
            // snapshot the working segment layers
            lock (this.store.segmentlayers) {
                 layers = this.store.segmentlayers.ToArray();
            }
            
            foreach (SegmentMemoryBuilder layer in layers) {

                INTERNAL_segmentWalkForNextKey(
                    lowkey,
                    layer,
                    RangeKey.newSegmentRangeKey(
                       new RecordKey().appendKeyPart("<"),
                       new RecordKey().appendKeyPart(">"),
                       num_generations),
                    handledIndexRecords,
                    num_generations,
                    recordsBeingAssembled,
                    equal_ok);
            }

            // now check the assembled records list
            try {
                KeyValuePair<RecordKey,RecordData> kvp = recordsBeingAssembled.FindNext(lowkey, true);
                key = kvp.Key;
                record = kvp.Value;
                return GetStatus.PRESENT;
            }
            catch (KeyNotFoundException) {
                return GetStatus.MISSING;
            }

        }


        class RangeKey 
        {
            RecordKey lowkey = null;
            RecordKey highkey = null;
            int generation;

            private RangeKey() {
            }

            public static RangeKey newSegmentRangeKey(RecordKey lowkey,RecordKey highkey, int generation) {
                RangeKey rk = new RangeKey();
                rk.lowkey = lowkey;
                rk.highkey = highkey;
                rk.generation = generation;
                return rk;

            }
            private static void verifyPart(string expected,string value) {
                if (!expected.Equals(value)) {
                    throw new Exception(String.Format("verify failed on RangeKey decode ({0} != {1})", expected, value));
                }
            }
            public static RangeKey decodeFromRecordKey(RecordKey existingkey) {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

                RangeKey rangekey = new RangeKey();
                // TODO, switch this to use a key PIPE!!
                verifyPart(".ROOT", existingkey.key_parts[0]);
                verifyPart("GEN", existingkey.key_parts[1]);
                
                rangekey.generation = (int)Lsd.lsdToNumber(enc.GetBytes(existingkey.key_parts[2]));
                rangekey.lowkey = new RecordKey(enc.GetBytes(existingkey.key_parts[3]));
                rangekey.highkey = new RecordKey(enc.GetBytes(existingkey.key_parts[4]));
                return rangekey;
            }
            public RecordKey toRecordKey() {
                if (lowkey == null || highkey == null) {
                    throw new Exception("no low/high keys for RangeKey.toRecordKey()");
                }
                RecordKey key = new RecordKey();
                key.appendParsedKey(".ROOT/GEN");
                key.appendKeyPart(Lsd.numberToLsd(generation, GEN_LSD_PAD));
                key.appendKeyPart(lowkey.encode());
                key.appendKeyPart(highkey.encode());
                return key;
            }

            public static bool isRangeKey(RecordKey key) {
                // .ROOT/GEN/X/lk/hk  == 5 parts
                if (key == null) {
                    throw new Exception("isRangeKey() handed a null key");
                }
                if (key.key_parts == null) {
                    throw new Exception("isRangeKey() handed a key with null key_parts");
                }
                if ( (key.key_parts.Count == 5)  &&
                     (key.key_parts[0].Equals(".ROOT")) &&
                     (key.key_parts[1].Equals("GEN"))) {
                    return true;
                } else {
                    return false;
                }
            }
            
            public bool directlyContainsKey(RecordKey testkey) {
                return true; //   TODO: fix the datavalues to all use "=" prefix encoding so our <> range tests work
                if ((this.lowkey.CompareTo(testkey) <= 0) &&
                    (this.highkey.CompareTo(testkey) >= 0)) {
                    return true;
                } else {
                    return false;
                }
            }
            public bool eventuallyContainsKey(RecordKey testkey) {
                return true; //   TODO: fix the datavalues to all use "=" prefix encoding so our <> range tests work
                // todo, recursively unpack the low-key/high-key until we no longer have a .ROOT/GEN formatted key
                // then find out if the supplied testkey is present in the final range
                if ((this.lowkey.CompareTo(testkey) <= 0) &&
                    (this.highkey.CompareTo(testkey) >= 0)) {
                    return true;
                } else {
                    return false;
                }

            }
            
            public static IEnumerable<KeyValuePair<RecordKey, RecordUpdate>> findAllElibibleRangeRows(
                IScannable<RecordKey, RecordUpdate> in_segment,
                RecordKey for_key,
                int for_generation) {
                // TODO: fix this prefix hack
                RecordKey startrk = new RecordKey()
                    .appendParsedKey(".ROOT/GEN")
                    .appendKeyPart(Lsd.numberToLsd(for_generation,GEN_LSD_PAD));
                IComparable<RecordKey> endrk = RecordKey.AfterPrefix(startrk);

                foreach (KeyValuePair<RecordKey, RecordUpdate> kvp 
                    in in_segment.scanForward(new ScanRange<RecordKey>(startrk, endrk, null))) {
                    if (!RangeKey.isRangeKey(kvp.Key)) {
                        System.Console.WriteLine("INTERNAL error, RangeKey scan found non-range key: " 
                            + kvp.Key.ToString() + " claimed to be before " + endrk.ToString() );
                        break;
                    }
                    if (kvp.Value.type == RecordUpdateTypes.FULL) {
                        yield return kvp;
                    }
                }
                
            }

            
        }
        
        private void INTERNAL_segmentWalkForNextKey(
            RecordKey startkeytest,
            ISortedSegment curseg_raw,
            RangeKey curseg_rangekey,
            IScannableDictionary<RecordKey, RecordData> handledIndexRecords,
            int maxgen,
            IScannableDictionary<RecordKey, RecordData> recordsBeingAssembled,
            bool equal_ok) {

            // TODO: convert all ISortedSegments to be IScannable
            IScannable<RecordKey, RecordUpdate> curseg = (IScannable<RecordKey, RecordUpdate>)curseg_raw;

            // first look in this segment for a next-key **IF** it may contain one
            if (curseg_rangekey.directlyContainsKey(startkeytest)) {
                KeyValuePair<RecordKey,RecordUpdate> nextrow;
                try {
                    nextrow = curseg.FindNext(startkeytest, equal_ok);
                    // we have a next record
                    RecordData partial_record;
                    if (!recordsBeingAssembled.TryGetValue(nextrow.Key, out partial_record)) {
                        partial_record = new RecordData(RecordDataState.NOT_PROVIDED, nextrow.Key);
                        recordsBeingAssembled[nextrow.Key] = partial_record;
                    }
                    partial_record.applyUpdate(nextrow.Value);
                } catch (KeyNotFoundException) {
                }
            }

            // find all generation range references that are relevant for this key
            // .. make a note of which ones are "current" 
            List<KeyValuePair<RecordKey,RecordUpdate>> todo_list= new List<KeyValuePair<RecordKey,RecordUpdate>>();
            for (int i = maxgen - 1; i >= 0; i--) {
                foreach (KeyValuePair<RecordKey, RecordUpdate> rangerow in RangeKey.findAllElibibleRangeRows(curseg, startkeytest, i)) {
                    // see if it is new for our handledIndexRecords dataset
                    RecordData partial_rangedata;
                    if (!handledIndexRecords.TryGetValue(rangerow.Key, out partial_rangedata)) {
                        partial_rangedata = new RecordData(RecordDataState.NOT_PROVIDED, rangerow.Key);
                        handledIndexRecords[rangerow.Key] = partial_rangedata;
                    }
                    if ((partial_rangedata.State == RecordDataState.INCOMPLETE) ||
                        (partial_rangedata.State == RecordDataState.NOT_PROVIDED)) {
                        // we're suppilying new data for this index record
                        partial_rangedata.applyUpdate(rangerow.Value);
                        // because we're suppilying new data, we should add this to our
                        // private TODO list if it is a FULL update, NOT a tombstone
                        if (rangerow.Value.type == RecordUpdateTypes.FULL) {
                            todo_list.Add(rangerow);
                        }
                    }
                }                    
            }


            // now repeat the walk through our todo list:
            foreach (KeyValuePair<RecordKey,RecordUpdate> rangepointer in todo_list) {

                SegmentReader next_seg = segmentReaderFromRow(rangepointer);

                RangeKey next_seg_rangekey = RangeKey.decodeFromRecordKey(rangepointer.Key);
                Debug.WriteLine("..WalkForNextKey descending to: " + rangepointer.Key);
                // RECURSE
                INTERNAL_segmentWalkForNextKey(
                    startkeytest,
                    next_seg,
                    next_seg_rangekey,
                    handledIndexRecords,
                    maxgen - 1,
                    recordsBeingAssembled,
                    equal_ok);
                    
            }
            // now repeat the walk of range references in this segment, this time actually descending
        }
        


        // ------------[ ** INTERNAL ** segmentWalkForKey ]-------
        //
        // This is the meaty internal function that does the "magic"
        // of the segment walk.
        
        private RecordUpdateResult INTERNAL_segmentWalkForKey_OLD(
            RecordKey key,
            ISortedSegment curseg,
            HashSet<int> handledGenerations,
            int maxgen,
            ref RecordData record) {

            // first look in this segment for the key
            {
                RecordUpdate update;
                if (curseg.getRecordUpdate(key, out update) == GetStatus.PRESENT) {
                    if (record.applyUpdate(update) == RecordUpdateResult.FINAL) {
                        return RecordUpdateResult.FINAL;
                    }
                }
            }

            // make a note of which generation range references have precedence in this segment
            HashSet<int> nextHandledGenerations = new HashSet<int>(handledGenerations);
            for (int i = maxgen; i >= 0; i--) {
                RecordKey rangekey = new RecordKey().appendParsedKey(".ROOT/GEN")
                    .appendKeyPart(Lsd.numberToLsd(i,GEN_LSD_PAD)).appendParsedKey("</>");
                RecordUpdate update;

                RangeKey trangekey = RangeKey.newSegmentRangeKey(
                   new RecordKey().appendParsedKey("<"),
                   new RecordKey().appendParsedKey(">"), i);
                if ((!trangekey.toRecordKey().Equals(rangekey))) {
                    throw new Exception(String.Format("RangeKey({0}) differs from RecordKey({1})", trangekey.toRecordKey(), rangekey));
                }

                // TODO: make a "getRecordExists()" call in ISortedSegment to make this more efficient
                //  .. then make sure we use that optimization to avoid calling getRecordUpdate on those
                //  .. entries in the next loop below.
                if (curseg.getRecordUpdate(rangekey,out update) == GetStatus.PRESENT) {
                    nextHandledGenerations.Add(i);  // mark the generation handled at this level
                }
            }

            // now repeat the walk of range references in this segment, this time actually descending
            for (int i = maxgen; i >= 0; i--) {
                
                RecordKey rangekey = new RecordKey().appendParsedKey(".ROOT/GEN")
                    .appendKeyPart(Lsd.numberToLsd(i,GEN_LSD_PAD)).appendParsedKey("</>");

               

                RecordUpdate update;  
                if (curseg.getRecordUpdate(rangekey,out update) == GetStatus.PRESENT) {
                    if (update.type == RecordUpdateTypes.FULL) {
                        
                        SegmentReader sr = segmentReaderFromRow(rangekey,update);

                        // RECURSE
                        if (INTERNAL_segmentWalkForKey_OLD(key, sr, nextHandledGenerations, maxgen - 1, ref record) == RecordUpdateResult.FINAL) {
                            return RecordUpdateResult.FINAL;
                        }
                    } else if (update.type == RecordUpdateTypes.DELETION_TOMBSTONE) {
                        // A tombstone for a rangemap means we can skip it!
                        // throw new Exception("TBD: implement handling for rangemap tombstones");
                    } else {
                        throw new Exception("Invalid rangerecord updatetype in walk: " + update.type.ToString());
                    }

                }
            }

            return RecordUpdateResult.SUCCESS;
        }




    }

}