#pragma once

#include "types.h"

inline uint16_t uint16_le(const byte bytes[])
{
	return bytes[0] | (bytes[1] << 8);
}
