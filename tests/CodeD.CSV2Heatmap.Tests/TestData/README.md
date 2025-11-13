# Test Data

This folder contains sample files for unit testing the XYZData class.

## File List

### sample_tab_separated.txt
- **Format**: Tab-separated values (TSV)
- **Header**: 2 header lines included
- **Data**: 3x2 grid data (X coordinates: 1-3, Y coordinates: 1-2)
- **Purpose**: Basic tab-separated file reading test

### sample_comma_separated.txt
- **Format**: Comma-separated values (CSV)
- **Header**: 2 header lines included
- **Data**: 3x2 grid data (X coordinates: 1-3, Y coordinates: 2,4)
- **Purpose**: Comma-separated file reading test

### sample_real_data.txt
- **Format**: Tab-separated values (TSV)
- **Header**: 1 header line included
- **Data**: 3x3 grid data (X coordinates: 0-2, Y coordinates: 0-2)
- **Purpose**: More complex real data reading test

### xyz_sample.txt / xyz_sample2.txt
- **Format**: Original files from samples folder
- **Purpose**: Reference only (currently not used in tests)

## Test Data Creation Guidelines

1. **File naming**: `sample_` prefix + descriptive name
2. **Format**: Test different delimiters (tab, comma, space)
3. **Header**: Test presence/absence and different patterns
4. **Data size**: Small and easily verifiable size
5. **Error cases**: Test invalid data and edge cases

## Usage

In test code, use the `GetTestFilePath(fileName)` method to access these files.