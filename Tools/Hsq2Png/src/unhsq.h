#pragma once

#include <istream>
#include <vector>

#include "types.h"

namespace hsq
{
	std::vector<byte> read_from_stream(std::istream& f);

	size_t get_unpacked_length(const byte* src, size_t srcLength);

	void unpack(const byte* src, size_t srcLength, byte* dest);
	std::vector<byte> unpack(const byte* src, size_t srcLength);
	std::vector<byte> unpack(const std::vector<byte>& src);

	std::vector<byte> unpack_from_stream(std::istream& f);
}