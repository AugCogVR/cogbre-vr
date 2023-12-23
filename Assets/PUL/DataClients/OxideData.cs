using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace PUL
{
    [System.Serializable]
    public class OxideData
    {
        public IList<OxideCollection> collectionList { get; set; }

        public OxideData()
        {
            collectionList = new List<OxideCollection>();
        }

        public override string ToString()
        {
            string output = "__OxideCollections__\n";

            //  Print out all collections
            foreach (OxideCollection collection in collectionList)
            {
                output += collection.ToString() + "\n";
            }

            return output;
        }
    }


    [System.Serializable]
    public class OxideCollection
    {
        public string collectionId { get; set; }
        public string name { get; set; }
        public string notes { get; set; }
        public IList<OxideBinary> binaryList { get; set; }

        public OxideCollection(string collectionId, string name, string notes, IList<OxideBinary> binaryList) 
        { 
            this.collectionId = collectionId;
            this.name = name;
            this.notes = notes;  // ???
            this.binaryList = binaryList;
        }

        public override string ToString()
        {
            string output = $"CID: {collectionId} || Name: {name} || Notes: {notes}";

            // Print out oids
            if ((binaryList != null) && (binaryList.Count > 0)) 
            {
                output += "\n\t->Binaries\n";
                foreach (OxideBinary binary in binaryList) 
                { 
                    output += "\t" + binary.ToString() + "\n";
                }
            }

            return output;
        }
    }

    // [System.Serializable]
    // public class Operand
    // {
    //     public class OperandMemory
    //     {
    //         public string memoryBase;
    //         public string displacement;
    //     }

    //     public string type_reg;
    //     public OperandMemory type_mem;
    //     public string type_imm;
    //     public int size;
    //     public string access;
    // }

    // [System.Serializable]
    // public class NexusValue
    // {
    //     public int id;
    //     public string mnemonic;
    //     public int address;
    //     public string op_str;
    //     public int size;
    //     public string str;
    //     public string[] groups;
    //     public string[] regs_read;
    //     public string[] regs_write;
    //     public List<List<string>> regs_access;
    //     public int[] prefix;
    //     public int[] opcode;
    //     public int rex;
    //     public int operand_size;
    //     public int modrm;
    //     public List<Operand> operands;
    // }

    [System.Serializable]
    public class OxideBinary
    {
        public string oid { get; set; }
        public string name { get; set; }
        public string size { get; set; }
        public Dictionary<string, string> disassemblyStringDict { get; set; }
        public IList<OxideFunction> functionList { get; set; }
        public IList<OxideBasicBlock> basicBlockList { get; set; }

        // Keep the next fields so that DisassemblyFormatter will compile. Not currently in use.
        // public IList<string> originalPaths { get; set; }
        public string dissasemblyPath { get; set; }
        public Dictionary<string, string> dissasembly { get; set; }
        public string dissasemblyOut { get; set; }

        public OxideBinary(string oid, string name, string size, 
        Dictionary<string, string> disassemblyStringDict, 
        IList<OxideFunction> functionList, IList<OxideBasicBlock> basicBlocklist
        /*IList<string> originalPaths, string dissasemblyPath,*/)
        {
            this.oid = oid;
            this.name = name;
            this.size = size;
            this.disassemblyStringDict = disassemblyStringDict;
            this.functionList = functionList;
            this.basicBlockList = basicBlockList;

            // this.originalPaths = originalPaths;
            // this.dissasemblyPath = dissasemblyPath;
            // dissasembly = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            string output = $"OID: {oid} || Name: {name} || Size: {size}";

            // TODO: add meaningful output for remaining fields

            // if(originalPaths.Count > 0) output += "\n\t\t-- > Original Paths\n";
            // // print out original paths
            // foreach (string path in originalPaths)
            // {
            //     output += "\t\t\t" + path + "\n";
            // }
            // output += $"\n\t\t-- > Disassembly: {dissasemblyPath}";

            return output;
        }
    }

    [System.Serializable]
    public class OxideFunction
    {
        public string name { get; set; }   
        public string offset { get; set; }
        public string signature { get; set; }
        public IList<OxideBasicBlock> basicBlockList { get; set; }

        public OxideFunction(string name, string offset, string signature, IList<OxideBasicBlock> basicBlockList)
        {
            this.name = name;
            this.offset = offset;
            this.signature = signature;
            this.basicBlockList = basicBlockList;
        }

        public override string ToString()
        {
            string output = $"Name: {name} || Offset: {offset} || Signature: {signature}";

            // TODO: add meaningful output for remaining fields

            return output;
        }
    }

    [System.Serializable]
    public class OxideBasicBlock
    {
        public string offset { get; set; }   
        public IList<string> instructionAddressList { get; set; }   
        public IList<string> destinationAddressList { get; set; }

        public OxideBasicBlock(string offset, IList<string> instructionAddressList, IList<string> destinationAddressList)
        {
            this.offset = offset;
            this.instructionAddressList = instructionAddressList;
            this.destinationAddressList = destinationAddressList;
        }

        public override string ToString()
        {
            string output = $"Offset: {offset}";

            // TODO: add meaningful output for remaining fields

            return output;
        }
    }
}
