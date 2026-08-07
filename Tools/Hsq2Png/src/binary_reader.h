#pragma once

#include <istream>
#include <vector>
#include "types.h"

class binary_reader
{
public:
	binary_reader(std::istream& is) : m_is(is) {}

	byte  read_byte();
	sbyte read_sbyte();

	uint16_t read_uint16_le();

	void read_bytes(byte* dest, size_t count);
	std::vector<byte> read_bytes(size_t count);

private:
	std::istream& m_is;
};