using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using PUL;

public class DisassemblyFormatter : MonoBehaviour
{
    public bool runParser = false;
    public NexusClient nexusClientInstance;
    StreamReader sr = null;

    // ! This method needs to be a lot faster. testing the sample program (roughly 26.5k elements) takes 5+ minutes to compile.
    // ! We need to think of a coherent way to cache the stored data.
    // - Luke 11/27/2023

    // Takes in an oid and parses the information from its file path into unity readable data
    public /*async*/ async void ParseDisassembly(OxideBinary OID)
    {
        if (!runParser) return;

        // -> Set the new stream reader path
        sr = new StreamReader(OID.dissasemblyPath);

        // -> Reset disasm out
        OID.dissasemblyOut = string.Empty;

        // -> Read information and write it to temp (Removes JSON formatting)
        string disasm = sr.ReadToEnd();
        disasm = disasm.Substring(1, disasm.Length - 2);

        // -> Parse through information and push it into the nexus object
        int n = 0;
        int overflowCheck = 10;
        bool lastCheck = false;
        while(n < overflowCheck)
        {
            // Check for early throw
            if (disasm.IndexOf(":") == -1) break;

            // Check for the last curly bracket
            // -> Every JSON chunk ends in }},
            int startingIndex = disasm.IndexOf(":") + 3;

            // -> Check if we grab normal index or if we are at the last statement
            int endingIndex = disasm.IndexOf("}}, ") + 1;
            if (endingIndex == 0)
            {
                endingIndex = disasm.Length - 1;
                lastCheck = true;
            }

            int cutLength = endingIndex - startingIndex;
            string chunk = disasm.Substring(startingIndex, cutLength);
            // -> Resize final
            disasm = lastCheck ? string.Empty : disasm.Substring(cutLength + startingIndex + 3);

            // -> Push information into a dictionary
            // --> Get int
            int aIndex = chunk.IndexOf("address") + 10;
            string pAddress = chunk.Substring(aIndex, chunk.IndexOf(",", aIndex) - aIndex);
            /*int address;
            if (!int.TryParse(pAddress, out address))
            {
                n++;
                continue;
            }*/
            // --> Get string
            int strIndex = chunk.IndexOf("\"str\"") + 8;
            string str = chunk.Substring(strIndex, chunk.IndexOf("\"", strIndex) - strIndex);

            //Communicate with NexusAPI.

            //A dictionary will be created inside the nexus API upon the loading of the disassembly. Once that is functional,
            //we need to create a synchronous nexus function that takes in an address, and returns the appropriate string value associated
            //with that address. I'm going home now, i'm tired. glhf, future Julian/Luke
            //11/26/23 - Julian
            // Seen :)

            // --> Add to dictionary
            OID.dissasemblyOut += $"{pAddress} - {str}\n";
            OID.dissasembly.Add(pAddress, str);

            //string currentDisasm = await nexusClientInstance.NexusSyncTask()



            // -> Old list parser
            #region List Parser
            // -> Reformat chunks into a json
            //NexusValue value = JsonUtility.FromJson<NexusValue>("{" + chunk + "}");
            //value.operands = new List<Operand>();



            // -> Parse through operands
            /*string operands = chunk.Substring(chunk.IndexOf("operands") + 11);
            while(n < overflowCheck)
            {
                // Check for an early throw
                if (operands.IndexOf("operand") == -1) break;

                // Chunks out operand information 
                int oStart = operands.IndexOf("operand") + 7;
                oStart = operands.IndexOf("{", oStart);
                int oEnd = operands.IndexOf("}", oStart) + 1;
                if(operands.IndexOf("type.mem") != -1 && operands.IndexOf("type.mem") < oEnd) oEnd = operands.IndexOf("}", oEnd) + 1;
                string oChunk = operands.Substring(oStart, oEnd - oStart);
                // -> Adjust chunk type reg name
                oChunk = oChunk.Replace("type.reg", "type_reg");
                oChunk = oChunk.Replace("type.mem", "type_mem");
                oChunk = oChunk.Replace("type.imm", "type_imm");
                oChunk = oChunk.Replace("base", "memoryBase");
                // -> Reduce the size of operands
                operands = operands.Substring(oEnd);

                // Pushes operand information
                // Debug.Log("CHUNK: O " + oChunk);
                Operand wOperand = JsonUtility.FromJson<Operand>(oChunk);
                value.operands.Add(wOperand);
                n++;
            }



            // -> Manually parse through regs access
            value.regs_access = new List<List<string>>();
            int rStart = chunk.IndexOf("regs_access") + 15;
            
            string regs = chunk.Substring(rStart, chunk.IndexOf("prefix") - 4 - rStart);
            // -> Force parse both lists
            for(int i = 0; i < 2; i++)
            {
                // Get start
                rStart = regs.IndexOf("[") + 1;
                // -> Get end based on case
                int rEnd = regs.IndexOf("]") + 1;
                string wRegs = regs.Substring(rStart, rEnd - rStart);
                regs = regs.Substring(rEnd);

                // -> Parse information
                List<string> caseList = new List<string>();

                // -> Manually push through all the data in the string
                while(n < overflowCheck)
                {
                    // Check if there are any more commas
                    if (wRegs.IndexOf(",") == -1) break;

                    // Pull string
                    caseList.Add(wRegs.Substring(0, wRegs.IndexOf(",") - 1));
                    wRegs = wRegs.Substring(wRegs.IndexOf(",") - 1);

                    n++;
                }

                value.regs_access.Add(caseList);
            }*/
            #endregion

            n++;
        }
        if (n >= overflowCheck) Debug.LogError("Overflow error");

        Debug.Log("Parse Checks: " + n);

        // Close writer and reader
        sr.Close();
    }
}
