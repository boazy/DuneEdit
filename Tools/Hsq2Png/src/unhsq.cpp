// Based on Roman Dolejsi's code from:
// http://bigs.fr/dune_old
//
// My own code is on the BSD 2-Clause license.

#include "binary_reader.h"
#include "endian.h"
#include "unhsq.h"

using namespace std;

namespace hsq
{

vector<byte> read_from_stream(istream& f)
{
	binary_reader br(f);
	f.seekg(3, ios_base::beg);

	// Get file size from offset 3
	size_t fileSize = br.read_uint16_le();
	f.seekg(0, ios_base::beg);

	// Allocate a vector with fileSize and read to it
	vector<byte> result(fileSize);
	br.read_bytes(result.data(), fileSize);

	return result;
}

int getbit(int *q, const byte** src)
{
    if (*q == 1) {
				
            *q = 0x10000 | uint16_le(*src);
            *src += 2;
    }
    if (*q & 1) {			// q is odd
            *q >>= 1;
            return 1;
    }
    else {					// q is even
            *q >>= 1;
            return 0;
    }
}

char unpack2(const byte *src, byte *dst)
{
	if (((src[0] + src[1] + src[2] + src[3] + src[4] + src[5]) & 0xff) != 171) return 0;
	int q = 1;

	src += 6;

	while (1)
	{
		if (getbit(&q, &src))
			*dst++ = *src++;
		else
		{
			int count;
			int offset;

			if (getbit(&q, &src))
			{
				count = *src & 7;

				offset = 0xffffe000 | (uint16_le(src) >> 3);

				src += 2;

				if (!count) count = *src++;


				if (!count)
					return 1;
			}
			else
			{
				count = getbit(&q, &src) << 1;
				count |= getbit(&q, &src);

				offset = 0xffffff00 | *src++;
			}

			count += 2;

			byte *dm = dst + offset;

			while (count--)
				*dst++ = *dm++;
		}
	}
}

size_t get_unpacked_length(const byte* src, size_t srcLength)
{
	if (srcLength < 6)
		throw runtime_error("HSQ Header not found.");

	 // Uncompressed file length can be found at offset 0 of the file
	return uint16_le(src);
}

void unpack(const byte* src, size_t srcLength, byte* dest)
{
	if (!unpack2(src, dest))
		throw runtime_error("HSQ Unpack failed.");
}

vector<byte> unpack(const byte* src, size_t srcLength)
{
	vector<byte> dest(get_unpacked_length(src, srcLength));
	unpack(src, srcLength, dest.data());
	return dest;
}

vector<byte> unpack(const vector<byte>& src)
{
	return unpack(src.data(), src.size());
}

vector<byte> unpack_from_stream(istream& f)
{
	return unpack(read_from_stream(f));
}

}