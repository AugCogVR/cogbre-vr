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
        public List<OxideCollection> collectionList { get; set; }

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
        public List<OxideBinary> binaryList { get; set; }

        public OxideCollection(string collectionId, string name, string notes, List<OxideBinary> binaryList) 
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

        // Dictionary of functions, indexed by starting offset
        public SortedDictionary<int, OxideFunction> functionDict { get; set; }

        // Dictionary of all basic blocks, indexed by starting offset
        public SortedDictionary<int, OxideBasicBlock> basicBlockDict { get; set; }

        // Dictionary of all disassembled instructions, indexed by offset
        public SortedDictionary<int, OxideInstruction> instructionDict { get; set; }

        // Dictionary of decompiled code for this whole binary, indexed by offset and line number. 
        public SortedDictionary<int, SortedDictionary<int, OxideDecompLine>> decompMapDict { get; set; }

        public OxideCollection parentCollection { get; set; }

        public OxideBinary(string oid, string name, string size)
        {
            this.oid = oid;
            this.name = name;
            this.size = size;
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

        // Basic blocks associated with this function, indexed by offset
        public SortedDictionary<int, OxideBasicBlock> basicBlockDict { get; set; }

        public List<string> paramsList { get; set; } 

        public string retType { get; set; }   

        public string signature { get; set; }

        public bool returning { get; set; }

        public OxideBinary parentBinary { get; set; }

        // What other functions call this one? indexed by offset
        public SortedDictionary<int, OxideFunction> sourceFunctionDict { get; set; }

        // What other functions does this one call? indexed by offset
        public SortedDictionary<int, OxideFunction> targetFunctionDict { get; set; }

        // Decomp lines associated with this function, indexed by line number
        public SortedDictionary<int, OxideDecompLine> decompDict { get; set; }

        // List of capabilities identified in this function
        public List<string> capaList { get; set; } 

        public OxideFunction(string name, string offset, string signature)
        {
            this.name = name;
            this.offset = offset;
            this.signature = signature;
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

        // Instructions associated with this basic block, indexed by offset
        public SortedDictionary<int, OxideInstruction> instructionDict { get; set; }

        // Source blocks of this block, indexed by offset
        public SortedDictionary<int, OxideBasicBlock> sourceBasicBlockDict { get; set; }

        // Destination blocks of this block, indexed by offset
        public SortedDictionary<int, OxideBasicBlock> targetBasicBlockDict { get; set; }

        // Keep destinations as strings because not all of them are offsets
        public List<string> destinationAddressList { get; set; }

        public OxideFunction parentFunction { get; set; } 

        public OxideBasicBlock(string offset)
        {
            this.offset = offset;
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
        
        // TODO: Add more instruction fields as needed! See commented-out code below.

        public OxideBasicBlock parentBlock { get; set; } 

        // Constructor only takes offset and string representation -- others fields will be set externally
        public OxideInstruction(string offset, string str)
        {
            this.offset = offset;
            this.str = str;
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

    [System.Serializable]
    public class OxideDecompLine
    {
        public string code { get; set; }   

        public SortedDictionary<int, OxideInstruction> associatedInstructionDict { get; set; }

        // Constructor only takes code -- others fields will be set externally
        public OxideDecompLine(string code)
        {
            this.code = code;
            this.associatedInstructionDict = null;
        }

        public override string ToString()
        {
            string output = $"Code: {code}";

            // TODO: add meaningful output for remaining fields

            return output;
        }
    }

}
