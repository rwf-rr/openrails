﻿// COPYRIGHT 2010, 2012 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MSTS;

namespace ORTS
{
    public class WSFile
    {
        public TR_WorldSoundFile TR_WorldSoundFile = null;

        public WSFile(string wsfilename)
        {
            Read(wsfilename);
        }

        public void Read(string wsfilename)
        {
            if (File.Exists(wsfilename))
            {
				Trace.Write("$");
                using (STFReader stf = new STFReader(wsfilename, false))
                {
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tr_worldsoundfile", ()=>{ TR_WorldSoundFile = new TR_WorldSoundFile(stf); }),
                    });
                    if (TR_WorldSoundFile == null)
                        STFException.TraceWarning(stf, "Missing TR_WorldSoundFile statement");
                }
            }
        }
    }

    public class TR_WorldSoundFile
    {
        public List<WorldSoundSource> SoundSources = new List<WorldSoundSource>();
        public List<WorldSoundRegion> SoundRegions = new List<WorldSoundRegion>();

        public TR_WorldSoundFile(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("soundsource", ()=>{ SoundSources.Add(new WorldSoundSource(stf)); }),
                new STFReader.TokenProcessor("soundregion", ()=>{ SoundRegions.Add(new WorldSoundRegion(stf)); }),
            });
        }
    }

    public class WorldSoundSource
    {
        public float X;
        public float Y;
        public float Z;
        public string SoundSourceFileName;

        public WorldSoundSource(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("filename", ()=>{ SoundSourceFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("position", ()=>{
                    stf.MustMatch("(");
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class WorldSoundRegion
    {
        public int SoundRegionTrackType = -1;
        public float ROTy;
        public List<int> TrackNodes;

        public WorldSoundRegion(STFReader stf)
        {
            TrackNodes = new List<int>();
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("soundregiontracktype", ()=>{ SoundRegionTrackType = stf.ReadIntBlock(-1); }),
                new STFReader.TokenProcessor("soundregionroty", ()=>{ ROTy = stf.ReadFloatBlock(STFReader.UNITS.None, float.MaxValue); }),
                new STFReader.TokenProcessor("tritemid", ()=>{
                    stf.MustMatch("(");
                    int dummy = stf.ReadInt(0);
                    dummy = stf.ReadInt(-1);
                    if (dummy != -1)
                        TrackNodes.Add(dummy);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }
}
