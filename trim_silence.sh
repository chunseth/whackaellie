#!/bin/bash

# Script to trim silence from the beginning of audio files
# Usage: ./trim_silence.sh input_file [output_file]

if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <input_file> [output_file]"
    echo "If output_file is not specified, it will overwrite the input file"
    exit 1
fi

INPUT_FILE="$1"
OUTPUT_FILE="${2:-$INPUT_FILE}"

if [ ! -f "$INPUT_FILE" ]; then
    echo "Error: Input file '$INPUT_FILE' not found"
    exit 1
fi

echo "Analyzing silence in: $INPUT_FILE"

# Detect where silence ends (silence_end is the time where silence stops)
SILENCE_START=$(ffmpeg -i "$INPUT_FILE" -af silencedetect=noise=-40dB:d=0.1 -f null - 2>&1 | \
    grep "silence_end" | \
    awk '{print $5}' | \
    head -1)

if [ -z "$SILENCE_START" ] || [ "$SILENCE_START" = "0" ] || [ "$SILENCE_START" = "0.0" ]; then
    echo "No significant silence detected at the beginning, or file starts with sound immediately"
    exit 0
fi

echo "Silence ends at: ${SILENCE_START}s"
echo "Trimming audio..."

# Determine output format based on file extension
EXT="${OUTPUT_FILE##*.}"
if [ "$EXT" = "wav" ]; then
    CODEC="-c:a pcm_s16le"
    FORMAT="-f wav"
elif [ "$EXT" = "m4a" ]; then
    CODEC="-c:a aac -b:a 192k"
    FORMAT="-vn"  # No video stream
else
    # Default to same format as input
    CODEC="-c:a copy"
    FORMAT=""
fi

# Create a temporary file with proper extension
TEMP_FILE="${OUTPUT_FILE%.*}.tmp.${EXT}"

# Trim the file from the silence end point
# Use atrim filter for accurate trimming
if ! ffmpeg -i "$INPUT_FILE" -af "atrim=start=${SILENCE_START}" $CODEC $FORMAT "$TEMP_FILE" -y >/dev/null 2>&1; then
    echo "Error: ffmpeg failed to trim the file"
    rm -f "$TEMP_FILE"
    exit 1
fi

# Check if the output file was created successfully
if [ -f "$TEMP_FILE" ] && [ -s "$TEMP_FILE" ]; then
    # If output is same as input, backup the original
    if [ "$OUTPUT_FILE" = "$INPUT_FILE" ]; then
        mv "$INPUT_FILE" "${INPUT_FILE}.bak"
        echo "Original file backed up as: ${INPUT_FILE}.bak"
    fi
    mv "$TEMP_FILE" "$OUTPUT_FILE"
    echo "Successfully trimmed! Output saved to: $OUTPUT_FILE"
    echo "Removed ${SILENCE_START}s of silence from the beginning"
else
    echo "Error: Failed to create trimmed file"
    rm -f "$TEMP_FILE"
    exit 1
fi

