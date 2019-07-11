import React from 'react';
import { storiesOf } from '@storybook/react';
import { action } from '@storybook/addon-actions';
import { BooleanValue } from 'react-values'
import { withKnobs, boolean, select, text } from '@storybook/addon-knobs/react';
import withReadme from 'storybook-readme/with-readme';
import Readme from './README.md';
import { ContentRow, Checkbox, Avatar, ContextMenuButton, Text } from 'asc-web-components';
import Section from '../../../.storybook/decorators/section';



storiesOf('Components|ContentRow', module)
  .addDecorator(withKnobs)
  .addDecorator(withReadme(Readme))
  .add('base', () => {

    const checkbox = boolean('checkbox', true);
    const avatar = boolean('avatar', true);
    const contextButton = boolean('contextButton', true);

    return(
      <Section>
        <ContentRow status=''
                    checkBox={checkbox 
                      ? <BooleanValue>
                          {({ value, toggle }) => (
                            <Checkbox isChecked={value} 
                                      onChange={e => {
                                        console.log('Item with id: '+ e.target.value + ' Checked!')
                                        toggle(e.target.checked);
                                      }} 
                                      isDisabled={false} 
                                      value='1' 
                                      id='1' />)}
                        </BooleanValue>
                      : '' }
                    avatar={avatar
                      ? <Avatar size='small' 
                                role='user' 
                                source=''
                                userName='' />
                      : ''}
                    contextButton={contextButton
                      ? <ContextMenuButton direction='right' 
                                        getData={() => 
                                          [
                                            {key: 'key1', label: 'Edit', onClick: () => console.log('Context action: Edit')},
                                            {key: 'key2', label: 'Delete', onClick: () => console.log('Context action: Delete')}
                                          ]} />
                      : ''}
              >
                <Text.Body truncate={true} >{text('content', '')}</Text.Body>
              </ContentRow>
      </Section>
    );
  });