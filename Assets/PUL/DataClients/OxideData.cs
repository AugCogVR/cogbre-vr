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

    [System.Serializable]
    public class OxideBinary
    {
        public string oid { get; set; }
        public string name { get; set; }
        public string size { get; set; }
        public SortedDictionary<int, OxideInstruction> instructionDict { get; set; }
        public SortedDictionary<int, OxideFunction> functionDict { get; set; }
        public SortedDictionary<int, OxideBasicBlock> basicBlockDict { get; set; }
        public SortedDictionary<int, string> decompilationDict  { get; set; }

        // Fields used by DisassemblyFormatter. Not currently in use.
        // public IList<string> originalPaths { get; set; }
        // public string dissasemblyPath { get; set; }
        // public Dictionary<string, string> dissasembly { get; set; }
        // public string dissasemblyOut { get; set; }

        public OxideBinary(string oid, string name, string size, 
        SortedDictionary<int, OxideInstruction> instructionDict, 
        SortedDictionary<int, OxideFunction> functionDict, 
        SortedDictionary<int, OxideBasicBlock> basicBlockDict,
        SortedDictionary<int, string> decompilationDict)
        {
            this.oid = oid;
            this.name = name;
            this.size = size;
            this.instructionDict = instructionDict;
            this.functionDict = functionDict;
            this.basicBlockDict = basicBlockDict;
            this.decompilationDict = decompilationDict;
        }

        public override string ToString()
        {
            string output = $"OID: {oid} || Name: {name} || Size: {size}";

            // TODO: add meaningful output for remaining fields

            return output;
        }
    }

    [System.Serializable]
    public class OxideFunction
    {
        public string name { get; set; }   
        public string offset { get; set; }  // aka "start" 
        public string vaddr { get; set; }   
        public SortedDictionary<int, OxideBasicBlock> basicBlockDict { get; set; }
        public IList<string> paramsList { get; set; } // aka "params" but that's a keyword in C#
        public string retType { get; set; }   
        public string signature { get; set; }
        public bool returning { get; set; }

        public OxideFunction(string name, string offset, string vaddr, 
        SortedDictionary<int, OxideBasicBlock> basicBlockDict, IList<string> paramsList,
        string retType, string signature, bool returning)
        {
            this.name = name;
            this.offset = offset;
            this.vaddr = vaddr;
            this.basicBlockDict = basicBlockDict;
            this.paramsList = paramsList;
            this.retType = retType;
            this.signature = signature;
            this.returning = returning;
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

    [System.Serializable]
    public class OxideInstruction
    {
        public string offset { get; set; }   
        public string str { get; set; }   
        public string mnemonic { get; set; }
        public string op_str { get; set; }
        // TODO: Add more fields as needed! See commented-out code below.

        // Constructor only takes offset -- others fields will be set externally
        public OxideInstruction(string offset)
        {
            this.offset = offset;
        }

        public override string ToString()
        {
            string output = $"Offset: {offset} || Instruction: {str}";

            // TODO: add meaningful output for remaining fields

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
}
