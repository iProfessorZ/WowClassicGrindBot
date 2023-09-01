﻿using System;
using System.Collections.Generic;
using System.IO;

namespace StormDll;

#nullable enable

internal sealed class Archive
{
    public const uint SFILE_INVALID_SIZE = 0xFFFFFFFF;

    private readonly IntPtr handle;

    private readonly HashSet<string> fileList = new(StringComparer.InvariantCultureIgnoreCase);

    private static readonly bool Is64Bit = Environment.Is64BitProcess;

    public Archive(string file, out bool open, uint prio, OpenArchive flags)
    {
        open = Is64Bit
            ? StormDllx64.SFileOpenArchive(file, prio, flags, out handle)
            : StormDllx86.SFileOpenArchive(file, prio, flags, out handle);

        if (!open)
            return;

        using MpqFileStream mpq = GetStream("(listfile)");
        using MemoryStream ms = new(mpq.ReadAllBytes());
        using StreamReader stream = new(ms);

        while (!stream.EndOfStream)
        {
            fileList.Add(stream.ReadLine()!);
        }

        if (fileList.Count == 0)
            throw new InvalidOperationException($"{nameof(fileList)} contains no elements!");
    }

    public bool IsOpen()
    {
        return handle != IntPtr.Zero;
    }

    public bool HasFile(string name)
    {
        return fileList.Contains(name);
    }

    public bool SFileCloseArchive()
    {
        fileList.Clear();
        return Is64Bit
            ? StormDllx64.SFileCloseArchive(handle)
            : StormDllx86.SFileCloseArchive(handle);
    }

    public MpqFileStream GetStream(string fileName)
    {
        return !SFileOpenFileEx(handle, fileName, OpenFile.SFILE_OPEN_FROM_MPQ, out IntPtr fileHandle)
            ? throw new IOException("SFileOpenFileEx failed")
            : new MpqFileStream(fileHandle);
    }

    public static bool SFileReadFile(IntPtr fileHandle, Span<byte> buffer, long toRead, out long read)
    {
        return Is64Bit
            ? StormDllx64.SFileReadFile(fileHandle, buffer, toRead, out read)
            : StormDllx86.SFileReadFile(fileHandle, buffer, toRead, out read);
    }

    public static bool SFileCloseFile(IntPtr fileHandle)
    {
        return Is64Bit
            ? StormDllx64.SFileCloseFile(fileHandle)
            : StormDllx86.SFileCloseFile(fileHandle);
    }

    public static long SFileGetFileSize(IntPtr fileHandle, out long fileSizeHigh)
    {
        return Is64Bit
            ? StormDllx64.SFileGetFileSize(fileHandle, out fileSizeHigh)
            : StormDllx86.SFileGetFileSize(fileHandle, out fileSizeHigh);
    }

    public static uint SFileSetFilePointer(IntPtr fileHandle,
        long filePos,
        ref uint plFilePosHigh,
        SeekOrigin origin)
    {
        return Is64Bit
            ? StormDllx64.SFileSetFilePointer(fileHandle, filePos, ref plFilePosHigh, origin)
            : StormDllx86.SFileSetFilePointer(fileHandle, filePos, ref plFilePosHigh, origin);
    }

    public static bool SFileOpenFileEx(
        IntPtr archiveHandle,
        string fileName,
        OpenFile searchScope,
        out IntPtr fileHandle)
    {
        return Is64Bit
            ? StormDllx64.SFileOpenFileEx(archiveHandle, fileName, searchScope, out fileHandle)
            : StormDllx86.SFileOpenFileEx(archiveHandle, fileName, searchScope, out fileHandle);
    }
}