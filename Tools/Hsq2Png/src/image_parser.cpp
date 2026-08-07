#include "image_parser.h"
#include "binary_reader.h"

#include <boost/foreach.hpp>
#include <iostream>

using namespace std;

struct sub_palette_header
{
	void clamp_length() 
	{
		if (static_cast<int>(start_color) + length > 256)
		{
			cerr << "Sub-palette too long: " << start_color << " + " << length << ". Clamping.";
			length = 256 - start_color;
		}
	}

	bool end_of_palette() const
	{
		return (start_color == 0xff) && (length == 0xff);
	}

	byte start_color;
	byte length;
};

class sub_palette
{
public:
	sub_palette(const palette& parent, byte offset, bool zeroTransparent) : parent(parent), offset(offset), zeroTransparent(zeroTransparent)
	{}

	rgb operator[](int index) const
	{
		//return rgb(index * 16, index * 16, index * 16);
		
		if (zeroTransparent && index == 0)
			return rgb(255, 0, 255, 0);
		else if (offset + index > 0xff)
		{
			cerr << "Index overflow: " << index;
			return rgb();
		}
		else
			return parent.colors[offset + index];
	}

private:
	const palette& parent;
	byte offset;
	bool zeroTransparent;
};

template <typename T>
inline static T read(binary_reader& br)
{}

#define DEFINE_READER(read_type) template <> inline static read_type read<read_type>(binary_reader& br)

DEFINE_READER(rgb)
{
	// RGB levels have only 6 bits of depth (6 bits per channel),
	// so they need to be shifted 2-bits (multiplied by 4).
	return rgb(br.read_byte() << 2, br.read_byte() << 2, br.read_byte() << 2);
}

DEFINE_READER(sub_palette_header)
{
	sub_palette_header sph;
	sph.start_color = br.read_byte();
	sph.length = br.read_byte();

	return sph;
}

inline void push_bipixel(sprite& spr, const sub_palette& sub_pal, byte bipixel, size_t& xpos)
{
	if (xpos < spr.width)
		spr.pixels.push_back(sub_pal[bipixel & 0x0f]);
	xpos++;

	if (xpos < spr.width)
		spr.pixels.push_back(sub_pal[bipixel >> 4]);
	xpos++;
}

sprite read_sprite(binary_reader& br, const palette& base_palette, bool transparent, bool no_palette)
{
	// RGB levels have only 6 bits of depth (6 bits per channel),
	// so they need to be shifted 2-bits (multiplied by 4).
	sprite spr;

	spr.width  = br.read_uint16_le();
	spr.height = br.read_byte();

	// Compression is stored as the MSB of the width
	bool compression = ((spr.width & 0x8000) == 0x8000);
	if (compression)
		spr.width &= ~0x8000;

	byte pal_offset = br.read_byte();

	sub_palette sub_pal(base_palette, pal_offset, transparent);

	// Read two unknown values that occur when there is no file palette
	if (no_palette)
	{
		br.read_byte();
		br.read_byte();
	}

	// Reserve expected number of pixels to avoid unnecessary reallocations.
	spr.pixels.reserve(spr.width * spr.height);

	if (compression)
	{
		size_t xpos = 0;
		size_t row = 0;

		while (row < spr.height)
		{
			sbyte repeat = br.read_sbyte();

			if (repeat < 0)
			{
				// Repeat the next bipixel |repeat|+1 times
				repeat--;
				byte bipixel = br.read_byte();

				for (; repeat < 0; repeat++)
					push_bipixel(spr, sub_pal, bipixel, xpos);
			}
			else
			{
				// Process the next |repeat|+1 bipixels without repetition
				for (repeat++; repeat > 0; repeat--)
				{
					push_bipixel(spr, sub_pal, br.read_byte(), xpos);
				}
			}

			if (xpos >= spr.width)
			{
				if (xpos % 4)
					br.read_bytes(4 - (xpos % 4));
				xpos = 0;
				row++;
			}
		}
	}
	else
	{
		for (size_t row = 0; row < spr.height; row++)
		{
			for (size_t xpos = 0; xpos < spr.width; )
			{
				// Sprite rows align on 16-bit word boundaries, so we need to read two bytes anyway.
				push_bipixel(spr, sub_pal, br.read_byte(), xpos);
				push_bipixel(spr, sub_pal, br.read_byte(), xpos);
			}
		}
	}

	return spr;
}

void image_parser::parse(istream& input, const std::function<void(const sprite&)>& sprite_reciever)
{
	binary_reader br(input);

	auto end_of_palette_offset = br.read_uint16_le();

	palette pal;

	// If end of palette is coming just after the offset itself, it means there is no file palette
	// (this happens in files where the sprites use external palettes).
	bool no_palette = (end_of_palette_offset == 2);

	if (no_palette)
	{
		// Set a dummy palette.
		for (size_t i = 0; i < 256; i++)
			pal.colors[i] = rgb(i, i, i);
	}
	else
	{
		// Read palette

		while (true)
		{
			auto sph = read<sub_palette_header>(br);
			if (sph.end_of_palette())
				break;

			sph.clamp_length();

			for (byte i = sph.start_color; i < sph.start_color + sph.length; i++)
				pal.colors[i] = read<rgb>(br);
		}
	}

	input.seekg(end_of_palette_offset);

	vector<uint16_t> sprite_offsets;
	sprite_offsets.push_back(br.read_uint16_le());
	size_t sprites_left = sprite_offsets[0] / 2;
	while (--sprites_left)
		sprite_offsets.push_back(br.read_uint16_le());

	BOOST_FOREACH(uint64_t sprite_offset, sprite_offsets)
	{
		input.seekg(end_of_palette_offset + sprite_offset);
		sprite spr = read_sprite(br, pal, transparent(), no_palette);
		if (spr.height)
			sprite_reciever(spr);
	}

}

image_parser::image_parser() : transparent_(false)
{

}
