﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace StormDll;

public sealed class MpqFileStream : Stream
{
    private const int ERROR_HANDLE_EOF = 38;

    private readonly long length;

    private nint fileHandle;
    private long position;

    public MpqFileStream(nint fileHandle)
    {
        this.fileHandle = fileHandle;

        long low = Archive.SFileGetFileSize(fileHandle, out long high);
        length = (high << 32) | low;
    }

    public sealed override bool CanRead => true;
    public sealed override bool CanSeek => true;
    public sealed override bool CanWrite => false;

    public sealed override long Length => length;

    public sealed override long Position
    {
        get => position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public sealed override void Flush() { }

    public sealed override int Read(byte[] buffer, int offset, int count)
    {
        if (offset > buffer.Length || (offset + count) > buffer.Length)
            throw new ArgumentException();

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        Span<byte> bufferSpan = buffer.AsSpan(offset);
        bool success = Archive.SFileReadFile(fileHandle, bufferSpan, count, out long bytesRead);
        position += bytesRead;

        if (!success)
        {
            int lastError = Marshal.GetLastWin32Error();
            if (lastError != ERROR_HANDLE_EOF)
                throw new Win32Exception(lastError);
        }

        return unchecked((int)bytesRead);
    }

    public sealed override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Current && offset < 0)
        {
            offset = Position + offset;
            origin = SeekOrigin.Begin;
        }

        uint low, high;
        low = unchecked((uint)(offset & 0xffffffffu));
        high = unchecked((uint)(offset >> 32));

        uint result = Archive.SFileSetFilePointer(fileHandle, low, ref high, origin);
        if (result == Archive.SFILE_INVALID_SIZE)
            throw new IOException("SFileSetFilePointer failed");

        return position = result;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public sealed override void Close()
    {
        base.Close();

        if (fileHandle == nint.Zero)
            return;

        Archive.SFileCloseFile(fileHandle);
        fileHandle = nint.Zero;
    }

    public byte[] ReadAllBytes()
    {
        byte[] bytes = new byte[Length];
        Read(bytes, 0, (int)Length);

        return bytes;
    }
}