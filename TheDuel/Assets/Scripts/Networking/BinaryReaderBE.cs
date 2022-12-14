using System;
using System.IO;
using UnityEngine;

public class BinaryReaderBE : BinaryReader { 
    public BinaryReaderBE(System.IO.Stream stream)  : base(stream) { }
    private static char[] arr = new char[255];
    
    public Matrix4x4 ReadMatrix4x4()
    {
        return new Matrix4x4(ReadVector4(), ReadVector4(), ReadVector4(), ReadVector4()).transpose;
    }  
    
    public Vector2 ReadVector2()
    {
        return new Vector2(ReadSingle(), ReadSingle());
    }      
    
    public Vector3 ReadVector3()
    {
        return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
    }      
    
    public Vector4 ReadVector4()
    {
        return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
    }

    public override string ReadString()
    {
        int length = ReadInt32();
        for (int c = 0; c < length; c++)
            arr[c] = ReadChar();
        var res = new string(arr, 0, length);
        return res;
    }
    
    public override int ReadInt32()
    {
        var data = base.ReadBytes(4);
        Array.Reverse(data);
        return BitConverter.ToInt32(data, 0);
    }

    public override short ReadInt16()
    {
        var data = base.ReadBytes(2);
        Array.Reverse(data);
        return BitConverter.ToInt16(data, 0);
    }


    public override float ReadSingle()
    {
        var data = base.ReadBytes(4);
        Array.Reverse(data);
        return BitConverter.ToSingle(data, 0);
    }
    
    public override double ReadDouble()
    {
        var data = base.ReadBytes(8);
        Array.Reverse(data);
        return BitConverter.ToDouble(data, 0);
    }

    public override char ReadChar()
    {
        var data = base.ReadBytes(2);
        Array.Reverse(data);
        return BitConverter.ToChar(data, 0);
    }
}

