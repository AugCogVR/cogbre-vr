using PUL2;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DisassemblyFormatter : MonoBehaviour
{
    StreamReader sr = null;

    // Takes in an oid and parses the information from its file path into unity readable data
    public /*async*/ void ParseDisassembly(NexusObject OID)
    {
        // -> Set the new stream reader path
        sr = new StreamReader(OID.dissasemblyPath);

        // -> Read information and write it to temp (Removes JSON formatting)
        string disasm = sr.ReadToEnd();
        disasm = disasm.Substring(1, disasm.Length - 2);

        // -> Parse through information and push it into the nexus object
        int chunkCount = 4;
        for(int i = 0; i < chunkCount; i++)
        {
            // Check for the last curly bracket
            // -> Every JSON chunk ends in }},
            int chunkLength = i != chunkCount - 1 ? disasm.IndexOf("}},", Mathf.FloorToInt(disasm.Length / chunkCount)) + 2 : disasm.Length;
            string chunk = disasm.Substring(0, chunkLength);
            disasm = disasm.Substring(chunkLength + 2);
        }


        // Close writer and reader
        sr.Close();
    }
}
