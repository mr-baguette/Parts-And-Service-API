using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnSAPI.AssetLoading
{
    /// <summary>Used for asset loading to determine where to get the file</summary>
    public struct ResourceType
    {
        /// <summary>Set this to the type your file is.</summary>
        public LoadType loadType;
        /// <summary>Location of the file</summary>
        public string path;
        /// <summary>Constructor used to initialise this struct.</summary>
        public ResourceType(string path, LoadType loadType)
        {
            this.path = path;
            this.loadType = loadType;
        }
    }
    /// <summary>Used to determine how to load a file</summary>
    public enum LoadType
    {
        /// <summary>
        /// Specifies that the resource is embedded in the assembly file. Path should be NAMESPACE(Includes subfolders).FILENAME.EXTENSION
        /// Recommended 
        /// </summary>
        Embedded,
        /// <summary>Specifies that the file is located somewhere within the game directory. Path should be relative to root directory.</summary>
        Disk,
        /// <summary>Specifies that the asset needs to be downloaded from the internet. Path should be the URL of the asset.</summary>
        Online
    }
}
