﻿using System;
using System.IO;

// TODO: raw file, raw partition region managers, tests

namespace Bend
{

    // -----------------[ IRegionManager ]---------------------------------------------------
    //
    // This exposes a linear address space called a Region. The LayerManager will divide this
    // Region into segments to hold the root block, log, and segments. We further maintain
    // the invariant that Segments are written in one sweep beginning at their start address. 
    // Once they are closed, they may be read any number of times, but they are disposed of 
    // before being written again. The log does not follow this pattern. 

    public interface IRegionManager
    {
        IRegion readRegionAddr(uint region_addr);
        IRegion writeRegionAddr(uint region_addr);
        void disposeRegionAddr(uint region_addr);
    }

    public interface IRegion
    {
        Stream getStream();
        long getStartAddress();
        long getSize();   // TODO: do something better with this, so we can't break
    }
   

    public interface IRegionWriter : IRegion
    {
        long getMaxSize();
    }
   
    // -----------------[ RegionExposedFiles ]-----------------------------------------------


    // manage a region of exposed files
    class RegionExposedFiles : IRegionManager
    {
        String dir_path;

        
        class EFRegion : IRegion
        {
            FileStream stream;
            long address;
            long length;
            internal EFRegion(long address, long length, FileStream stream) {
                this.address = address;
                this.length = length;
                this.stream = stream;
            }

            public Stream getStream() {
                return stream;
            }

            public long getStartAddress() {
                return address;
            }
            public long getSize() {
                return stream.Length;
            }
        }
       
        public RegionExposedFiles(String location) {
            this.dir_path = location;
        }

        // first time init        
        public RegionExposedFiles(InitMode mode, String location) : this(location) {
            if (mode != InitMode.NEW_REGION) {
                throw new Exception("first time init needs NEW_REGION paramater");
            }
            if (!Directory.Exists(dir_path)) {
                Console.WriteLine("LayerManager, creating directory: " + dir_path);
                Directory.CreateDirectory(dir_path);
            }
        }

        private String makeFilepath(uint region_addr) {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            String addr = enc.GetString(Lsd.numberToLsd((int)region_addr,13));
            String filepath = dir_path + String.Format("\\addr{0}.reg", addr);
            return filepath;
        }
        // impl ...

        public IRegion readRegionAddr(uint region_addr) {
            String filepath = makeFilepath(region_addr);
            if (File.Exists(filepath)) {
                FileStream reader = File.Open(filepath, FileMode.Open);
                return new EFRegion(region_addr, reader.Length, reader);
            } else {
                return new EFRegion(-1, 0, null);
            }
        }
        public IRegion writeRegionAddr(uint region_addr) {
            FileStream writer = File.Open(makeFilepath(region_addr), FileMode.OpenOrCreate);
            return new EFRegion(region_addr,-1,writer);
        }
        public void disposeRegionAddr(uint region_addr) {
            String filepath = this.makeFilepath(region_addr);
            String del_filename = String.Format("\\del{0}addr{1}.region", DateTime.Now.ToBinary(), region_addr);
            File.Move(filepath, dir_path + del_filename);
        }
    }
}