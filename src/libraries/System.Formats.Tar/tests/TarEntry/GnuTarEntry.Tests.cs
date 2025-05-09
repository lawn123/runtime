// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class GnuTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<ArgumentException>(() => new GnuTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.V7RegularFile, InitialEntryName));

            // These are specific to GNU, but currently the user cannot create them manually
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.TapeVolume, InitialEntryName));

            // The user should not create these entries manually
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.LongPath, InitialEntryName));
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            GnuTarEntry regularFile = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            GnuTarEntry directory = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            GnuTarEntry hardLink = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            GnuTarEntry symbolicLink = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            GnuTarEntry blockDevice = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            GnuTarEntry characterDevice = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            GnuTarEntry fifo = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);

            if (canSeek)
            {
                ms.Position = actualEntry.DataOffset;
                byte actualData = (byte)ms.ReadByte();
                Assert.Equal(ExpectedOffsetDataSingleByte, actualData);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);

            if (canSeek)
            {
                ms.Position = actualEntry.DataOffset;
                byte actualData = (byte)ms.ReadByte();
                Assert.Equal(ExpectedOffsetDataSingleByte, actualData);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_LongPath(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, veryLongName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // GNU first writes the long path entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_LongPath_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, veryLongName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // GNU first writes the long path entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_LongLink(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                entry.LinkName = new string('a', 1234); // Forces using a GNU LongLink entry
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_LongLink_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                entry.LinkName = new string('b', 1234); // Forces using a GNU LongLink entry
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_LongLink_LongPath(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongName);
                entry.LinkName = new string('b', 1234); // Forces using a GNU LongLink entry
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The data section starts on the next byte.
            long expectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_LongLink_LongPath_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongName);
                entry.LinkName = new string('b', 1234); // Forces using a GNU LongLink entry
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The data section starts on the next byte.
            long expectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Fact]
        public void DataOffset_BeforeAndAfterArchive()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);
            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
            entry.DataStream.Position = 0; // The data stream is written to the archive from the current position

            using MemoryStream ms = new();
            using TarWriter writer = new(ms);
            writer.WriteEntry(entry);
            Assert.Equal(512, entry.DataOffset);

            // Write it again, the offset should now point to the second written entry
            // First entry 512 (header) + 1 (data) + 511 (padding)
            // Second entry 512 (header)
            // 512 + 512 + 512 = 1536
            writer.WriteEntry(entry);
            Assert.Equal(1536, entry.DataOffset);
        }

        [Fact]
        public async Task DataOffset_BeforeAndAfterArchive_Async()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);

            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
            entry.DataStream.Position = 0; // The data stream is written to the archive from the current position

            await using MemoryStream ms = new();
            await using TarWriter writer = new(ms);
            await writer.WriteEntryAsync(entry);
            Assert.Equal(512, entry.DataOffset);

            // Write it again, the offset should now point to the second written entry
            // First entry 512 (header) + 1 (data) + 511 (padding)
            // Second entry 512 (header)
            // 512 + 512 + 512 = 1536
            await writer.WriteEntryAsync(entry);
            Assert.Equal(1536, entry.DataOffset);
        }

        [Fact]
        public void DataOffset_UnseekableDataStream()
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);

                using MemoryStream dataStream = new();
                dataStream.WriteByte(ExpectedOffsetDataSingleByte);
                dataStream.Position = 0;
                using WrappedStream wds = new(dataStream, canWrite: true, canRead: true, canSeek: false);
                entry.DataStream = wds;

                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using TarReader reader = new(ms);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);
            // Gnu header length is 512, data starts in the next position
            Assert.Equal(512, actualEntry.DataOffset);
        }

        [Fact]
        public async Task DataOffset_UnseekableDataStream_Async()
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);

                await using MemoryStream dataStream = new();
                dataStream.WriteByte(ExpectedOffsetDataSingleByte);
                dataStream.Position = 0;
                await using WrappedStream wds = new(dataStream, canWrite: true, canRead: true, canSeek: false);
                entry.DataStream = wds;

                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using TarReader reader = new(ms);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);
            // Gnu header length is 512, data starts in the next position
            Assert.Equal(512, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_LongPath_LongLink_SecondEntry(bool canSeek)
        {
            string veryLongPathName = new string('a', 1234); // Forces using a GNU LongPath entry
            string veryLongLinkName = new string('b', 1234); // Forces using a GNU LongLink entry

            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry1 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry1.LinkName = veryLongLinkName;
                writer.WriteEntry(entry1);

                GnuTarEntry entry2 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry2.LinkName = veryLongLinkName;
                writer.WriteEntry(entry2);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry firstEntry = reader.GetNextEntry();
            Assert.NotNull(firstEntry);
            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The regular file data section starts on the next byte.
            long firstExpectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);

            TarEntry secondEntry = reader.GetNextEntry();
            Assert.NotNull(secondEntry);
            // First entry (including its long link and long path entries) end at 4608 (no padding, empty, as symlink has no data)
            // Second entry (including its long link and long path entries) data section starts one byte after 4608 + 4608 = 9216
            long secondExpectedDataOffset = canSeek ? 9216 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_LongPath_LongLink_SecondEntry_Async(bool canSeek)
        {
            string veryLongPathName = new string('a', 1234); // Forces using a GNU LongPath entry
            string veryLongLinkName = new string('b', 1234); // Forces using a GNU LongLink entry

            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry1 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry1.LinkName = veryLongLinkName;
                await writer.WriteEntryAsync(entry1);

                GnuTarEntry entry2 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry2.LinkName = veryLongLinkName;
                await writer.WriteEntryAsync(entry2);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry firstEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(firstEntry);
            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The regular file data section starts on the next byte.
            long firstExpectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);

            TarEntry secondEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(secondEntry);
            // First entry (including its long link and long path entries) end at 4608 (no padding, empty, as symlink has no data)
            // Second entry (including its long link and long path entries) data section starts one byte after 4608 + 4608 = 9216
            long secondExpectedDataOffset = canSeek ? 9216 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }

        [Fact]
        public void UnusedBytesInSizeFieldShouldBeZeroChars()
        {
            // The GNU format sets the unused bytes in the size field to "0" characters.

            using MemoryStream ms = new();

            using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry without data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(0, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, 0);

            ms.SetLength(0); // Reset the stream
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }; // 8 bytes of data means a size of 10 in octal
            using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry with data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream(data);
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(data.Length, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, data.Length);
        }

        [Fact]
        public async Task UnusedBytesInSizeFieldShouldBeZeroChars_Async()
        {
            // The GNU format sets the unused bytes in the size field to "0" characters.

            await using MemoryStream ms = new();

            await using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry without data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(0, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, 0);

            ms.SetLength(0); // Reset the stream
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }; // 8 bytes of data means a size of 10 in octal
            await using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry with data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream(data);
                await writer.WriteEntryAsync(entry);
            }

            ms.Position = 0;
            await using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(data.Length, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, data.Length);
        }

        private void ValidateUnusedBytesInSizeField(MemoryStream ms, long size)
        {
            // internally, the unused bytes in the size field should be "0" characters,
            // and the rest should be the octal value of the size field

            // name, mode, uid, gid,
            // size
            int sizeStart = 100 + 8 + 8 + 8;
            byte[] buffer = new byte[12]; // The size field is 12 bytes in length

            ms.Seek(sizeStart, SeekOrigin.Begin);
            ms.Read(buffer);

            // Convert the base 10 value of size to base 8
            string octalSize = Convert.ToString(size, 8).PadLeft(11, '0');
            byte[] octalSizeBytes = Encoding.ASCII.GetBytes(octalSize);
            // The last byte should be a null character
            Assert.Equal(octalSizeBytes, buffer.Take(octalSizeBytes.Length).ToArray());
        }

        [Fact]
        public void ATime_CTime_Epochs_ShouldBeNulls()
        {
            // The GNU format sets the access and change times to nulls when they are set to the unix epoch.

            using MemoryStream ms = new();

            using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Should not be "0" characters, but null characters
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName)
                {
                    AccessTime = DateTimeOffset.UnixEpoch,
                    ChangeTime = DateTimeOffset.UnixEpoch
                };
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            // Publicly they should be the unix epoch
            using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(DateTimeOffset.UnixEpoch, entry.AccessTime);
                Assert.Equal(DateTimeOffset.UnixEpoch, entry.ChangeTime);
            }

            ValidateATimeAndCTimeBytes(ms);
        }

        [Fact]
        public async Task ATime_CTime_Epochs_ShouldBeNulls_Async()
        {
            await using MemoryStream ms = new();

            await using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Should not be "0" characters, but null characters
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName)
                {
                    AccessTime = DateTimeOffset.UnixEpoch,
                    ChangeTime = DateTimeOffset.UnixEpoch
                };
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            // Publicly they should be the unix epoch
            await using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(DateTimeOffset.UnixEpoch, entry.AccessTime);
                Assert.Equal(DateTimeOffset.UnixEpoch, entry.ChangeTime);
            }

            ValidateATimeAndCTimeBytes(ms);
        }

        private void ValidateATimeAndCTimeBytes(MemoryStream ms)
        {
            // internally, atime and ctime should be nulls

            // name, mode, uid, gid, size, mtime, checksum, typeflag, linkname, magic, uname, gname, devmajor, devminor,
            // atime, ctime
            int aTimeStart = 100 + 8 + 8 + 8 + 12 + 12 + 8 + 1 + 100 + 8 + 32 + 32 + 8 + 8;
            int cTimeStart = aTimeStart + 12;
            byte[] buffer = new byte[12]; // atime and ctime are 12 bytes in length

            ms.Seek(aTimeStart, SeekOrigin.Begin);
            ms.Read(buffer);

            Assert.All(buffer, b => Assert.Equal(0, b)); // All should be nulls

            ms.Seek(cTimeStart, SeekOrigin.Begin);
            ms.Read(buffer);
            Assert.All(buffer, b => Assert.Equal(0, b)); // All should be nulls
        }
    }
}
