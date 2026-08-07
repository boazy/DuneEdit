#include "binary_reader.h"
#include "endian.h"

using namespace std;

byte binary_reader::read_byte()
{
	byte b;
	read_bytes(&b, 1);
	return b;
}

sbyte binary_reader::read_sbyte()
{
	byte b;
	read_bytes(&b, 1);
	return reinterpret_cast<const sbyte&>(b);
}

void binary_reader::read_bytes(byte* dest, size_t count)
{
	m_is.read(reinterpret_cast<char*>(dest), count);
}

vector<byte> binary_reader::read_bytes(size_t count)
{
	vector<byte> dest(count);
	read_bytes(dest.data(), count);
	return dest;
}

uint16_t binary_reader::read_uint16_le()
{
	byte b[2];
	read_bytes(b, 2);
	return uint16_le(b);
}
